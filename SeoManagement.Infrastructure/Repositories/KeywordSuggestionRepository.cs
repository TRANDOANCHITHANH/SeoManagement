using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class KeywordSuggestionRepository : IKeywordSuggestionRepository
	{
		private readonly AppDbContext _context;
		public KeywordSuggestionRepository(AppDbContext context)
		{
			_context = context;
		}
		public async Task<List<KeywordSuggestion>> GetByProjectIdAsync(int projectId)
		{
			return await _context.KeywordSuggestions
				.Include(ks => ks.MonthlySearchVolumes)
				.Where(u => u.ProjectID == projectId)
				.AsSplitQuery()
				.ToListAsync() ?? new List<KeywordSuggestion>();
		}
		public async Task<List<KeywordSuggestion>> GetSuggestionsAsync(int projectId, string seedKeyword)
		{
			return await _context.KeywordSuggestions
				.Where(ks => ks.ProjectID == projectId && ks.SeedKeyword == seedKeyword)
				.ToListAsync();
		}

		public async Task AddSuggestionsAsync(List<KeywordSuggestion> suggestions)
		{
			await _context.KeywordSuggestions.AddRangeAsync(suggestions);
			await _context.SaveChangesAsync();
		}

		public Task<KeywordSuggestion> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}

		public async Task AddAsync(KeywordSuggestion entity)
		{
			await _context.KeywordSuggestions.AddAsync(entity);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(KeywordSuggestion entity)
		{
			var existingResult = await _context.KeywordSuggestions.FindAsync(entity.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(entity);
				existingResult.CreatedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.KeywordSuggestions.FindAsync(id);
			if (result != null)
			{
				_context.KeywordSuggestions.Remove(result);
				await _context.SaveChangesAsync();
			}
		}
	}
}
