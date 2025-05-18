using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IApiKeyRepository : IRepository<ApiKey>
	{
		Task<List<ApiKey>> GetAllAsync();
		Task<List<ApiKey>> GetByServiceNameAsync(string serviceName);
		Task<ApiKey> GetActiveKeyAsync(string serviceName);
	}
}
