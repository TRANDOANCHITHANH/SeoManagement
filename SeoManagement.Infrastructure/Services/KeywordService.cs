using Microsoft.Extensions.Configuration;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using System.Text.Json;

namespace SeoManagement.Infrastructure.Services
{
	public class KeywordService : IService<Keyword>
	{
		private readonly IKeywordRepository _repository;
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;

		public KeywordService(IKeywordRepository repository, HttpClient httpClient, IConfiguration configuration)
		{
			_repository = repository;
			_httpClient = httpClient;
			_apiKey = configuration["RapidApiKey"];
			_httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "ahrefs1.p.rapidapi.com");
			_httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", _apiKey);
		}

		public async Task AddAsync(Keyword entity)
		{
			await _repository.AddAsync(entity);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
		}

		public async Task<List<Keyword>> GetByProjectIdAsync(int id)
		{
			return await _repository.GetByProjectIdAsync(id);
		}

		public async Task UpdateAsync(Keyword entity)
		{
			await _repository.UpdateAsync(entity);
		}

		public async Task<Keyword> GetAndSaveKeywordRankAsync(string keyword, string domain, int projectId, string country = "vn")
		{
			if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(domain))
			{
				throw new ArgumentException("Keyword and domain cannot be empty.", nameof(keyword));
			}

			var encodedKeyword = Uri.EscapeDataString(keyword);
			var requestUri = $"https://ahrefs1.p.rapidapi.com/v1/keyword-rank-checker?keyword={encodedKeyword}&domain={domain}&country={country}";
			var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
			using (var response = await _httpClient.SendAsync(request))
			{
				response.EnsureSuccessStatusCode();
				var json = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"API Response for keyword {keyword} on domain {domain}: {json}");

				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;

				var keywordEntity = new Keyword
				{
					ProjectID = projectId,
					KeywordName = keyword,
					Domain = domain,
					TopPosition = root.TryGetProperty("topPosition", out var topPosition) && topPosition.TryGetProperty("pos", out var pos) && pos.ValueKind == JsonValueKind.Number ? pos.GetInt32() : (int?)null,
					TopVolume = root.TryGetProperty("topPosition", out var topPos) && topPos.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 1 && content[1].ValueKind == JsonValueKind.Object && content[1].TryGetProperty("link", out var link) && link.ValueKind == JsonValueKind.Array && link.GetArrayLength() > 1 && link[1].ValueKind == JsonValueKind.Object && link[1].TryGetProperty("metrics", out var metricsProp) && metricsProp.TryGetProperty("topVolume", out var topVolumeProp) && topVolumeProp.ValueKind == JsonValueKind.Number ? topVolumeProp.GetInt32() : (int?)null,
					SerpResultsJson = root.TryGetProperty("serp", out var serp) && serp.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array
						? JsonSerializer.Serialize(results.EnumerateArray().Select(r =>
						{
							JsonElement? contentArray = r.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.Array && contentProp.GetArrayLength() > 1 ? contentProp : null;
							JsonElement? linkArray = contentArray.HasValue && contentArray.Value[1].ValueKind == JsonValueKind.Object && contentArray.Value[1].TryGetProperty("link", out var linkProp) && linkProp.ValueKind == JsonValueKind.Array && linkProp.GetArrayLength() > 1 ? linkProp : null;

							var result = new Dictionary<string, object>
							{
								{ "Position", r.TryGetProperty("pos", out var posProp) && posProp.ValueKind == JsonValueKind.Number ? posProp.GetInt32() : null },
								{ "Title", linkArray.HasValue && linkArray.Value[1].ValueKind == JsonValueKind.Object && linkArray.Value[1].TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null },
								{ "Url", linkArray.HasValue && linkArray.Value[1].ValueKind == JsonValueKind.Object && linkArray.Value[1].TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.Array && urlProp.GetArrayLength() > 1 ? urlProp[1].GetProperty("url").GetString() : null }
							};

							// Chỉ thêm Metrics nếu có dữ liệu và các giá trị không null
							if (linkArray.HasValue && linkArray.Value[1].ValueKind == JsonValueKind.Object && linkArray.Value[1].TryGetProperty("metrics", out var metricsProp) && metricsProp.ValueKind == JsonValueKind.Object)
							{
								var metrics = new Dictionary<string, object>();
								if (metricsProp.TryGetProperty("rank", out var rankProp) && rankProp.ValueKind == JsonValueKind.Number)
									metrics["Rank"] = rankProp.GetInt64();
								if (metricsProp.TryGetProperty("domainRating", out var drProp) && drProp.ValueKind == JsonValueKind.Number)
									metrics["DomainRating"] = drProp.GetInt32();
								if (metricsProp.TryGetProperty("traffic", out var trafficProp) && trafficProp.ValueKind == JsonValueKind.Number)
									metrics["Traffic"] = trafficProp.GetInt32();
								if (metricsProp.TryGetProperty("topVolume", out var tvProp) && tvProp.ValueKind == JsonValueKind.Number)
									metrics["TopVolume"] = tvProp.GetInt32();

								if (metrics.Count > 0)
									result["Metrics"] = metrics;
							}

							return result;
						}))
						: "[]",
					LastUpdate = root.TryGetProperty("lastUpdate", out var lastUpdateProp) && DateTime.TryParse(lastUpdateProp.GetString(), out var lastUpdate) ? lastUpdate : DateTime.UtcNow
				};

				var existingKeyword = (await _repository.GetByProjectIdAsync(projectId))
					.FirstOrDefault(r => r.KeywordName == keyword && r.Domain == domain);
				if (existingKeyword != null)
				{
					existingKeyword.TopPosition = keywordEntity.TopPosition;
					existingKeyword.TopVolume = keywordEntity.TopVolume;
					existingKeyword.SerpResultsJson = keywordEntity.SerpResultsJson;
					existingKeyword.LastUpdate = keywordEntity.LastUpdate;
					await _repository.UpdateAsync(existingKeyword);
					return existingKeyword;
				}

				await _repository.AddAsync(keywordEntity);
				return keywordEntity;
			}
		}
	}
}
