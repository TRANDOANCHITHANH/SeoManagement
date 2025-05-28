using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class SEOProjectRepository : ISEOProjectRepository
	{
		private readonly AppDbContext _context;
		private IDbContextTransaction _transaction;

		public SEOProjectRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, int? userId = null)
		{
			var query = _context.SEOProjects
								.Include(p => p.Keywords)
								.Include(p => p.Backlinks)
								.Include(p => p.AlertConfiguration)
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
				.Include(p => p.AlertConfiguration)
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
				var alertConfig = new AlertConfiguration
				{
					ProjectId = project.ProjectID,
					IsAlertMonitored = false
				};
				await _context.AlertConfigurations.AddAsync(alertConfig);
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
			var existingProject = await _context.SEOProjects
	.Include(p => p.AlertConfiguration)
	.FirstOrDefaultAsync(p => p.ProjectID == project.ProjectID);

			if (existingProject != null)
			{
				// Cập nhật các thuộc tính chính
				existingProject.IsMonitored = project.IsMonitored;
				existingProject.ProjectName = project.ProjectName;
				existingProject.Description = project.Description;
				existingProject.ProjectType = project.ProjectType;
				existingProject.StartDate = project.StartDate;
				existingProject.EndDate = project.EndDate;
				existingProject.Status = project.Status;
				existingProject.UserId = project.UserId;

				// Cập nhật AlertConfiguration
				if (existingProject.AlertConfiguration != null)
				{
					if (project.AlertConfiguration != null)
					{
						existingProject.AlertConfiguration.IsAlertMonitored = project.AlertConfiguration.IsAlertMonitored;
					}
				}
				else if (project.AlertConfiguration != null)
				{
					// Nếu AlertConfiguration không tồn tại, tạo mới và thêm vào DbContext
					var alertConfig = new AlertConfiguration
					{
						ProjectId = project.ProjectID,
						IsAlertMonitored = project.AlertConfiguration.IsAlertMonitored
					};
					existingProject.AlertConfiguration = alertConfig;
					_context.AlertConfigurations.Add(alertConfig);
				}

				await _context.SaveChangesAsync();
			}
			else
			{
				throw new Exception($"Dự án với ID {project.ProjectID} không tồn tại.");
			}
		}

		public async Task DeleteAsync(int projectId)
		{
			var project = await _context.SEOProjects.FindAsync(projectId);
			if (project != null)
			{
				if (project.AlertConfiguration != null)
				{
					_context.AlertConfigurations.Remove(project.AlertConfiguration);
				}
				_context.SEOProjects.Remove(project);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<IEnumerable<SEOProject>> GetAllAsync(string projectType)
		{
			var query = await _context.SEOProjects.
				Include(p => p.AlertConfiguration).
				Where(p => p.ProjectType == projectType)
				.AsNoTracking().ToListAsync();
			return query;
		}

		public async Task BeginTransactionAsync()
		{
			if (_transaction != null)
			{
				await _transaction.DisposeAsync();
			}
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitTransactionAsync()
		{
			if (_transaction != null)
			{
				await _transaction.CommitAsync();
				await _transaction.DisposeAsync();
				_transaction = null;
			}
		}

		public async Task RollbackTransactionAsync()
		{
			if (_transaction != null)
			{
				await _transaction.RollbackAsync();
				await _transaction.DisposeAsync();
				_transaction = null;
			}
		}
	}
}
