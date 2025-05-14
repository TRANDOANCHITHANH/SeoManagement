using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class PageSpeedResultRepository : IPageSpeedResultRepository
	{
		private readonly AppDbContext _context;

		public PageSpeedResultRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(PageSpeedResult pageSpeedResult)
		{
			await _context.PageSpeedResults.AddAsync(pageSpeedResult);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(PageSpeedResult pageSpeedResult)
		{
			var existingResult = await _context.PageSpeedResults.FindAsync(pageSpeedResult.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(pageSpeedResult);
				existingResult.LastCheckedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<PageSpeedResult>> GetByProjectIdAsync(int projectId)
		{
			return await _context.PageSpeedResults
				.Where(u => u.ProjectID == projectId)
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.PageSpeedResults.FindAsync(id);
			if (result != null)
			{
				_context.PageSpeedResults.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<PageSpeedResult> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}
	}
}
