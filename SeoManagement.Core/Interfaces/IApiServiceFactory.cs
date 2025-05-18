namespace SeoManagement.Core.Interfaces
{
	public interface IApiServiceFactory
	{
		Task<(HttpClient Client, string ApiKey, string SearchEngineId)> CreateGoogleCustomSearchClientAsync();
		Task<(HttpClient Client, string ApiKey)> CreatePageSpeedClientAsync();
		Task<HttpClient> CreateRapidApiClientAsync(string rapidApiHost);
	}
}
