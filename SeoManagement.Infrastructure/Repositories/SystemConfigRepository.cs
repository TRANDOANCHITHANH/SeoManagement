using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class SystemConfigRepository : ISystemConfigRepository
	{
		private readonly AppDbContext _context;
		public SystemConfigRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(SystemConfig config)
		{
			await _context.SystemConfigs.AddAsync(config);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(SystemConfig config)
		{
			var existingConfig = await _context.SystemConfigs.FindAsync(config.ConfigID);
			if (existingConfig != null)
			{
				_context.Entry(existingConfig).CurrentValues.SetValues(config);
				existingConfig.LastModified = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task DeleteAsync(int configId)
		{
			var config = await _context.SystemConfigs.FindAsync(configId);
			if (config != null)
			{
				_context.SystemConfigs.Remove(config);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<SystemConfig> GetByIdAsync(int configId)
		{
			return await _context.SystemConfigs.FindAsync(configId);
		}

		public async Task<(List<SystemConfig> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			if (pageNumber < 1 || pageSize < 1)
				throw new ArgumentException("PageNumber and PageSize must be positive.");

			var query = _context.SystemConfigs.AsQueryable();
			query = query.OrderBy(c => c.ConfigKey);

			var totalItems = await query.CountAsync();
			var items = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalItems);
		}
	}
}
