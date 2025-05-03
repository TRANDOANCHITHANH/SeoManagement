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

		public async Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, int? userId = null)
		{
			var query = _context.SEOProjects
								.Include(p => p.Keywords)
								.Include(p => p.Backlinks)
								.OrderBy(p => p.ProjectID)
								.AsNoTracking();

			if (userId.HasValue)
			{
				query = query.Where(p => p.UserId == userId.Value);
			}
			var totalItems = await query.CountAsync();
			var items = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			return (items, totalItems);
		}

		public async Task<SEOProject> GetByIdAsync(int projectId)
		{
			var project = await _context.SEOProjects
				.Include(p => p.Keywords)
				.Include(p => p.Backlinks)
				.FirstOrDefaultAsync(p => p.ProjectID == projectId);

			if (project == null)
			{
				throw new KeyNotFoundException($"Không tìm thấy project với ID {projectId}.");
			}
			return project;
		}

		public async Task AddAsync(SEOProject project)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				var userExists = await _context.Users.AnyAsync(u => u.Id == project.UserId);
				if (!userExists)
				{
					throw new InvalidOperationException($"User with ID {project.UserId} does not exist.");
				}
				await _context.SEOProjects.AddAsync(project);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task UpdateAsync(SEOProject project)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				_context.SEOProjects.Update(project);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
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
