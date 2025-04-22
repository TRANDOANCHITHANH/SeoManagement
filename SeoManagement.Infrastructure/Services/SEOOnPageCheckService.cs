using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Services
{
	public class SEOOnPageCheckService : ISEOOnPageCheckService
	{
		private readonly AppDbContext _context;

		public SEOOnPageCheckService(AppDbContext appDbContext)
		{
			_context = appDbContext;
		}

		public async Task<(List<SEOOnPageCheck> Items, int TotalItems)> GetPagedAsync(int projectId, int pageNumber, int pageSize)
		{
			var query = _context.SEOOnPageChecks
				.Where(c => c.ProjectID == projectId);

			var totalItems = await query.CountAsync();
			var items = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalItems);
		}

		public async Task<SEOOnPageCheck> GetByIdAsync(int id)
		{
			return await _context.SEOOnPageChecks.Include(c => c.Project)
				.FirstOrDefaultAsync(c => c.CheckID == id);
		}

		public async Task CreateSEOOnPageCheckAsync(SEOOnPageCheck check)
		{
			_context.SEOOnPageChecks.Add(check);
			await _context.SaveChangesAsync();
		}


		public async Task DeleteSEOOnPageCheckAsync(int id)
		{
			var check = await _context.SEOOnPageChecks.FindAsync(id);
			if (check != null)
			{
				_context.SEOOnPageChecks.Remove(check);
				await _context.SaveChangesAsync();
			}
		}


		public async Task UpdateSEOOnPageCheckAsync(SEOOnPageCheck check)
		{
			_context.Entry(check).State = EntityState.Modified;
			await _context.SaveChangesAsync();
		}
	}
}
