using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class ApiServiceFactory : IApiServiceFactory
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IApiKeyService _apiKeyService;

		public ApiServiceFactory(IHttpClientFactory httpClientFactory, IApiKeyService apiKeyService)
		{
			_httpClientFactory = httpClientFactory;
			_apiKeyService = apiKeyService;
		}


		public async Task<(HttpClient Client, string ApiKey, string SearchEngineId)> CreateGoogleCustomSearchClientAsync()
		{
			var apiKey = await _apiKeyService.GetActiveApiKeyAsync("GoogleCustomSearch:ApiKey");
			var searchEngineId = await _apiKeyService.GetActiveApiKeyAsync("GoogleCustomSearch:SearchEngineId");
			var httpClient = _httpClientFactory.CreateClient();
			return (httpClient, apiKey, searchEngineId);
		}

		public async Task<(HttpClient Client, string ApiKey)> CreatePageSpeedClientAsync()
		{
			var apiKey = await _apiKeyService.GetActiveApiKeyAsync("GooglePageSpeed");
			var httpClient = _httpClientFactory.CreateClient();
			return (httpClient, apiKey);
		}

		public async Task<HttpClient> CreateRapidApiClientAsync(string rapidApiHost)
		{
			var apiKey = await _apiKeyService.GetActiveApiKeyAsync("RapidApiKey");
			var httpClient = _httpClientFactory.CreateClient();
			httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", rapidApiHost);
			httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
			return httpClient;
		}
	}
}
