using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class SEOProjectRepository : ISEOProjectRepository
	{
		private readonly AppDbContext _context;

		public SEOProjectRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			var query = _context.SEOProjects.AsQueryable();
			var totalItems = await query.CountAsync();
			var items = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			return (items, totalItems);
		}

		public async Task<SEOProject> GetByIdAsync(int projectId)
		{
			return await _context.SEOProjects.FindAsync(projectId);
		}

		public async Task AddAsync(SEOProject project)
		{
			await _context.SEOProjects.AddAsync(project);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(SEOProject project)
		{
			_context.SEOProjects.Update(project);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int projectId)
		{
			var project = await _context.SEOProjects.FindAsync(projectId);
			if (project != null)
			{
				_context.SEOProjects.Remove(project);
				await _context.SaveChangesAsync();
			}
		}
	}
}
