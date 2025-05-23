using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class ContentOptimizationRepository : IContentOptimizationRepository
	{
		private readonly AppDbContext _context;
		public ContentOptimizationRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(ContentOptimizationAnalysis model)
		{
			await _context.ContentOptimizationAnalyses.AddAsync(model);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(ContentOptimizationAnalysis model)
		{
			var existingResult = await _context.ContentOptimizationAnalyses.FindAsync(model.Id);
			if (existingResult != null)
			{
				_context.Entry(existingResult).CurrentValues.SetValues(model);
				existingResult.AnalyzedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<ContentOptimizationAnalysis>> GetByProjectIdAsync(int projectId)
		{
			return await _context.ContentOptimizationAnalyses
				.Where(a => a.ProjectID == projectId)
				.OrderByDescending(a => a.AnalyzedAt)
				.ToListAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var result = await _context.ContentOptimizationAnalyses.FindAsync(id);
			if (result != null)
			{
				_context.ContentOptimizationAnalyses.Remove(result);
				await _context.SaveChangesAsync();
			}
		}

		public Task<ContentOptimizationAnalysis> GetByIdAsync(int id)
		{
			throw new NotImplementedException();
		}
	}
}
