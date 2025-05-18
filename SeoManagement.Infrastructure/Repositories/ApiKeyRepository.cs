using Microsoft.EntityFrameworkCore;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;

namespace SeoManagement.Infrastructure.Repositories
{
	public class ApiKeyRepository : IApiKeyRepository
	{
		private readonly AppDbContext _context;
		public ApiKeyRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<List<ApiKey>> GetAllAsync()
		{
			return await _context.ApiKeys.ToListAsync();
		}

		public async Task<List<ApiKey>> GetByServiceNameAsync(string serviceName)
		{
			return await _context.ApiKeys
				.Where(k => k.ServiceName == serviceName)
				.ToListAsync();
		}

		public async Task<ApiKey> GetByIdAsync(int id)
		{
			return await _context.ApiKeys.FindAsync(id);
		}

		public async Task AddAsync(ApiKey apiKey)
		{
			await _context.ApiKeys.AddAsync(apiKey);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(ApiKey apiKey)
		{
			_context.ApiKeys.Update(apiKey);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var apiKey = await GetByIdAsync(id);
			if (apiKey != null)
			{
				_context.ApiKeys.Remove(apiKey);
				await _context.SaveChangesAsync();
			}
		}

		public async Task<ApiKey> GetActiveKeyAsync(string serviceName)
		{
			return await _context.ApiKeys
				.Where(k => k.ServiceName == serviceName && k.IsActive)
				.FirstOrDefaultAsync();
		}
	}
}
