using SeoManagement.Core.Interfaces;
using System.Text.Json;

namespace SeoManagement.Infrastructure.Services
{
	public class GoogleCustomSearchService
	{
		private readonly IApiServiceFactory _apiServiceFactory;

		public GoogleCustomSearchService(IApiServiceFactory apiServiceFactory)
		{
			_apiServiceFactory = apiServiceFactory;
		}

		public async Task<bool> CheckIfIndexedAsync(string url)
		{
			try
			{
				var (_httpClient, _apiKey, _searchEngineId) = await _apiServiceFactory.CreateGoogleCustomSearchClientAsync();
				var query = $"site:{url}";
				var requestUrl = $"https://www.googleapis.com/customsearch/v1?key={_apiKey}&cx={_searchEngineId}&q={Uri.EscapeDataString(query)}";

				var response = await _httpClient.GetAsync(requestUrl);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<GoogleSearchResponse>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return result.Items != null && result.Items.Length > 0;
			}
			catch (Exception ex)
			{
				throw new Exception("Lỗi khi gọi Google Custom Search API: " + ex.Message, ex);
			}
		}
	}

	public class GoogleSearchResponse
	{
		public GoogleSearchItem[] Items { get; set; }
	}

	public class GoogleSearchItem
	{
		public string Link { get; set; }
	}
}
