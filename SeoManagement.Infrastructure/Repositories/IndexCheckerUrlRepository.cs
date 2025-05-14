using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class IndexCheckerUrlRepository : IIndexCheckerUrlRepository
	{
		private readonly AppDbContext _context;
		public IndexCheckerUrlRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(IndexCheckerUrl url)
		{
			await _context.IndexCheckerUrls.AddAsync(url);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(IndexCheckerUrl url)
		{
			var existingUrl = await _context.IndexCheckerUrls.FindAsync(url.UrlID);
			if (existingUrl != null)
			{
				_context.Entry(existingUrl).CurrentValues.SetValues(url);
				existingUrl.LastCheckedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<IndexCheckerUrl>> GetByProjectIdAsync(int projectId)
		{
			return await _context.IndexCheckerUrls
				.Where(u => u.ProjectID == projectId)
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var indexCheckerUrl = await _context.IndexCheckerUrls.FindAsync(id);
			if (indexCheckerUrl != null)
			{
				_context.IndexCheckerUrls.Remove(indexCheckerUrl);
				await _context.SaveChangesAsync();
			}
		}

		public Task<IndexCheckerUrl> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}
	}
}
