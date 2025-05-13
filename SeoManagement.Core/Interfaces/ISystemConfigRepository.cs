using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISystemConfigRepository : IRepository<SystemConfig>
	{
		Task<(List<SystemConfig> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
	}
}
