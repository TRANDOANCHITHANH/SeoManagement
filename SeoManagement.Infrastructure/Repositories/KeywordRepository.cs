using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class KeywordRepository : IKeywordRepository
	{
		private readonly AppDbContext _context;

		public KeywordRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<(IEnumerable<Keyword>, int)> GetPagedByProjectIdAsync(int projectId, int pageNumber, int pageSize)
		{
			var query = _context.Keywords
				.Where(k => k.ProjectID == projectId)
				.OrderByDescending(k => k.CreatedDate);

			var totalItems = await query.CountAsync();
			var items = await query
				.Include(k => k.KeywordHistories)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalItems);
		}

		public async Task<Keyword> GetByIdAsync(int id)
		{
			return await _context.Keywords
				.Include(k => k.Project)
				.Include(k => k.KeywordHistories)
				.FirstOrDefaultAsync(k => k.KeywordID == id);
		}

		public async Task AddAsync(Keyword keyword)
		{
			await _context.Keywords.AddAsync(keyword);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Keyword keyword)
		{
			var existingKeyword = await GetByIdAsync(keyword.KeywordID);
			if (existingKeyword == null) return;

			existingKeyword.KeywordName = keyword.KeywordName;
			existingKeyword.SearchVolume = keyword.SearchVolume;
			existingKeyword.Competition = keyword.Competition;
			existingKeyword.SearchIntent = keyword.SearchIntent;

			if (existingKeyword.CurrentRank != keyword.CurrentRank && keyword.CurrentRank.HasValue)
			{
				var history = new KeywordHistory
				{
					KeywordID = keyword.KeywordID,
					Rank = keyword.CurrentRank.Value,
					RecordedDate = DateTime.Now
				};
				await AddKeywordHistoryAsync(history);
			}

			existingKeyword.CurrentRank = keyword.CurrentRank;

			_context.Keywords.Update(existingKeyword);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var keyword = await GetByIdAsync(id);
			if (keyword != null)
			{
				_context.Keywords.Remove(keyword);
				await _context.SaveChangesAsync();
			}
		}

		public async Task AddKeywordHistoryAsync(KeywordHistory history)
		{
			await _context.KeywordHistories.AddAsync(history);
			await _context.SaveChangesAsync();
		}
	}
}
