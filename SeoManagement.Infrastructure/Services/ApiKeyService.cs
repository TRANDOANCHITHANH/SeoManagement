using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class ApiKeyService : IApiKeyService
	{
		private readonly IApiKeyRepository _apiKeyRepository;
		private readonly EncryptionService _encryptionService;

		public ApiKeyService(IApiKeyRepository apiKeyRepository, EncryptionService encryptionService)
		{
			_apiKeyRepository = apiKeyRepository;
			_encryptionService = encryptionService;
		}

		public async Task<List<ApiKey>> GetAllAsync()
		{
			return await _apiKeyRepository.GetAllAsync();
		}

		public async Task<List<(ApiKey ApiKey, string DecryptedKeyValue)>> GetAllWithDecryptedKeysAsync()
		{
			var apiKeys = await _apiKeyRepository.GetAllAsync();
			return apiKeys.Select(k => (k, _encryptionService.Decrypt(k.EncryptedKeyValue))).ToList();
		}

		public async Task<List<ApiKey>> GetByServiceNameAsync(string serviceName)
		{
			return await _apiKeyRepository.GetByServiceNameAsync(serviceName);
		}

		public async Task<(ApiKey ApiKey, string DecryptedKeyValue)> GetByIdAsync(int id)
		{
			var apiKey = await _apiKeyRepository.GetByIdAsync(id);
			if (apiKey == null) return (null, null);
			return (apiKey, _encryptionService.Decrypt(apiKey.EncryptedKeyValue));
		}

		public async Task AddAsync(ApiKey apiKey, string plainKeyValue)
		{
			apiKey.CreatedDate = DateTime.UtcNow;
			apiKey.EncryptedKeyValue = _encryptionService.Encrypt(plainKeyValue);
			await _apiKeyRepository.AddAsync(apiKey);
		}

		public async Task UpdateAsync(ApiKey apiKey, string plainKeyValue)
		{
			apiKey.EncryptedKeyValue = _encryptionService.Encrypt(plainKeyValue);
			await _apiKeyRepository.UpdateAsync(apiKey);
		}

		public async Task DeleteAsync(int id)
		{
			await _apiKeyRepository.DeleteAsync(id);
		}

		public async Task<string> GetActiveApiKeyAsync(string serviceName)
		{

			var activeKey = await _apiKeyRepository.GetActiveKeyAsync(serviceName);

			if (activeKey == null || (activeKey.ExpiryDate.HasValue && activeKey.ExpiryDate.Value < DateTime.UtcNow))
			{
				var keys = await _apiKeyRepository.GetByServiceNameAsync(serviceName);
				activeKey = keys.FirstOrDefault(k => !k.ExpiryDate.HasValue || k.ExpiryDate.Value >= DateTime.UtcNow);

				if (activeKey != null)
				{
					activeKey.IsActive = true;
					activeKey.LastUsedDate = DateTime.UtcNow;
					await UpdateAsync(activeKey, _encryptionService.Decrypt(activeKey.EncryptedKeyValue));
				}
				else
				{
					throw new Exception($"No valid API keys available for service {serviceName}.");
				}
			}

			activeKey.LastUsedDate = DateTime.UtcNow;
			await UpdateAsync(activeKey, _encryptionService.Decrypt(activeKey.EncryptedKeyValue));
			return _encryptionService.Decrypt(activeKey.EncryptedKeyValue);
		}
	}
}
