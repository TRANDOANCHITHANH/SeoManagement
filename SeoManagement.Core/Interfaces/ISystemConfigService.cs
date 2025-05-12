using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISystemConfigService
	{
		Task<(List<SystemConfig> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<SystemConfig> GetByIdAsync(int configId);
		Task CreateAsync(SystemConfig config);
		Task UpdateAsync(SystemConfig config);
		Task DeleteAsync(int configId);
	}
}
