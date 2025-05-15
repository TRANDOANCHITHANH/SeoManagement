using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class WebsiteInsightRepository : IWebsiteInsightRepository
	{
		private readonly AppDbContext _context;

		public WebsiteInsightRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(WebsiteInsight result)
		{
			await _context.WebsiteInsights.AddAsync(result);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(WebsiteInsight result)
		{
			var existingResult = await _context.WebsiteInsights.FindAsync(result.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(result);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<WebsiteInsight>> GetByProjectIdAsync(int projectId)
		{
			return await _context.WebsiteInsights
				.Where(u => u.ProjectID == projectId)
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.WebsiteInsights.FindAsync(id);
			if (result != null)
			{
				_context.WebsiteInsights.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<WebsiteInsight> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}
	}
}
