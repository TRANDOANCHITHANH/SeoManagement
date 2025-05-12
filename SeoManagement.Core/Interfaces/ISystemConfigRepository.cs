using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISystemConfigRepository
	{
		Task AddAsync(SystemConfig config);
		Task UpdateAsync(SystemConfig config);
		Task DeleteAsync(int configId);
		Task<SystemConfig> GetByIdAsync(int configId);
		Task<(List<SystemConfig> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
	}
}
