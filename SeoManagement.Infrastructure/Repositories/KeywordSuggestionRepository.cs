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
		public async Task<List<SeedKeyword>> GetByProjectIdAsync(int projectId)
		{
			return await _context.SeedKeywords
				.Include(sk => sk.RelatedKeywords)
				.Where(u => u.ProjectID == projectId)
				.AsSplitQuery()
				.ToListAsync() ?? new List<SeedKeyword>();
		}
		public async Task<List<SeedKeyword>> GetSuggestionsAsync(int projectId, string seedKeyword)
		{
			return await _context.SeedKeywords
				.Where(ks => ks.ProjectID == projectId && ks.Keyword == seedKeyword)
				.ToListAsync();
		}

		public async Task AddSuggestionsAsync(List<SeedKeyword> suggestions)
		{
			await _context.SeedKeywords.AddRangeAsync(suggestions);
			await _context.SaveChangesAsync();
		}

		public Task<SeedKeyword> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}

		public async Task AddAsync(SeedKeyword entity)
		{
			await _context.SeedKeywords.AddAsync(entity);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(SeedKeyword entity)
		{
			var existingResult = await _context.SeedKeywords.FindAsync(entity.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(entity);
				existingResult.CreatedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.SeedKeywords.FindAsync(id);
			if (result != null)
			{
				_context.SeedKeywords.Remove(result);
				await _context.SaveChangesAsync();
			}
		}
	}
}
