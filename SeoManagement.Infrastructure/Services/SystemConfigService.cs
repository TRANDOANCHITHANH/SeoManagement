using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class SystemConfigService : ISystemConfigService
	{
		private readonly ISystemConfigRepository _repository;
		public SystemConfigService(ISystemConfigRepository repository)
		{
			_repository = repository;
		}

		public async Task<(List<SystemConfig> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize)
		{
			return await _repository.GetPagedAsync(pageNumber, pageSize);
		}

		public async Task<SystemConfig> GetByIdAsync(int configId)
		{
			return await _repository.GetByIdAsync(configId);
		}

		public async Task CreateAsync(SystemConfig config)
		{
			if (string.IsNullOrWhiteSpace(config.ConfigKey))
				throw new ArgumentException("ConfigKey is required.");

			await _repository.AddAsync(config);
		}

		public async Task UpdateAsync(SystemConfig config)
		{
			var existingConfig = await _repository.GetByIdAsync(config.ConfigID);
			if (existingConfig == null)
				throw new KeyNotFoundException("Config not found.");

			await _repository.UpdateAsync(config);
		}

		public async Task DeleteAsync(int configId)
		{
			var config = await _repository.GetByIdAsync(configId);
			if (config == null)
				throw new KeyNotFoundException("Config not found.");

			await _repository.DeleteAsync(configId);
		}
	}
}
