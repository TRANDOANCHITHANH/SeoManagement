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

		public async Task AddAsync(Keyword entity)
		{
			await _context.Keywords.AddAsync(entity);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.Keywords.FindAsync(id);
			if (result != null)
			{
				_context.Keywords.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<Keyword> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}

		public async Task<List<Keyword>> GetByProjectIdAsync(int projectId)
		{
			return await _context.Keywords
			.Where(b => b.ProjectID == projectId)
			.Include(k => k.KeywordHistories)
			.AsSplitQuery()
			.ToListAsync();
		}

		public async Task UpdateAsync(Keyword entity)
		{
			var existingResult = await _context.Keywords.FindAsync(entity.KeywordID);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(entity);
				existingResult.LastUpdate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<Keyword> GetByKeywordAndDomainAsync(int projectId, string keyword, string domain)
		{
			return await _context.Keywords
				.Include(k => k.KeywordHistories)
				.FirstOrDefaultAsync(k => k.ProjectID == projectId && k.KeywordName == keyword && k.Domain == domain);
		}
	}
}
