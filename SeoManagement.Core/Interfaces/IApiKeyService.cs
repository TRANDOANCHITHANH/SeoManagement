using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IApiKeyService
	{
		Task<List<ApiKey>> GetAllAsync();
		Task<List<(ApiKey ApiKey, string DecryptedKeyValue)>> GetAllWithDecryptedKeysAsync();
		Task<List<ApiKey>> GetByServiceNameAsync(string serviceName);
		Task<(ApiKey ApiKey, string DecryptedKeyValue)> GetByIdAsync(int id);
		Task AddAsync(ApiKey apiKey, string plainKeyValue);
		Task UpdateAsync(ApiKey apiKey, string plainKeyValue);
		Task DeleteAsync(int id);
		Task<string> GetActiveApiKeyAsync(string serviceName);
	}
}
