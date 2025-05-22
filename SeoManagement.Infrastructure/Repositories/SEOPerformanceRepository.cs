using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class SEOPerformanceRepository : ISEOPerformanceRepository
	{
		private readonly AppDbContext _context;

		public SEOPerformanceRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(SEOPerformanceHistory model)
		{
			await _context.SEOPerformanceHistories.AddAsync(model);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(SEOPerformanceHistory model)
		{
			var existingResult = await _context.SEOPerformanceHistories.FindAsync(model.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(model);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<SEOPerformanceHistory>> GetByProjectIdAsync(int projectId)
		{
			return await _context.SEOPerformanceHistories
				.Where(u => u.ProjectId == projectId)
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.SEOPerformanceHistories.FindAsync(id);
			if (result != null)
			{
				_context.SEOPerformanceHistories.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<SEOPerformanceHistory> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}

		public async Task<List<SEOPerformanceHistory>> GetHistoryByProjectIdAsync(int projectId)
		{
			return await _context.SEOPerformanceHistories
				.Where(h => h.ProjectId == projectId)
				.OrderByDescending(h => h.RecordedAt)
				.ToListAsync();
		}
	}
}
