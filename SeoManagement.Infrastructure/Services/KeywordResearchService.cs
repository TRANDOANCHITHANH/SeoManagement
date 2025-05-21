using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeoManagement.Core.Common;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;

namespace SeoManagement.Infrastructure.Services
{
	public class KeywordResearchService : IKeywordResearchService
	{
		private readonly IKeywordSuggestionRepository _keywordSuggestionRepository;
		private readonly IApiServiceFactory _apiServiceFactory;
		private readonly IMemoryCache _memoryCache;
		private readonly ILogger<KeywordResearchService> _logger;
		public KeywordResearchService(IKeywordSuggestionRepository keywordSuggestionRepository, IApiServiceFactory apiServiceFactory, IMemoryCache memoryCache, ILogger<KeywordResearchService> logger)
		{
			_keywordSuggestionRepository = keywordSuggestionRepository;
			_apiServiceFactory = apiServiceFactory;
			_memoryCache = memoryCache;
			_logger = logger;
		}

		public async Task AddAsync(SeedKeyword entity)
		{
			await _keywordSuggestionRepository.AddAsync(entity);
		}

		public async Task DeleteAsync(int id)
		{
			await _keywordSuggestionRepository.DeleteAsync(id);
		}

		public async Task<List<SeedKeyword>> GetByProjectIdAsync(int id)
		{
			return await _keywordSuggestionRepository.GetByProjectIdAsync(id);
		}

		public async Task<List<SeedKeyword>> ResearchKeywordsAsync(int projectId, string seedKeyword)
		{
			var cacheKey = $"KeywordResearch_{projectId}_{seedKeyword}";
			if (_memoryCache.TryGetValue(cacheKey, out List<SeedKeyword> cachedSuggestions))
			{
				return cachedSuggestions;
			}

			var existingSuggestions = await _keywordSuggestionRepository.GetSuggestionsAsync(projectId, seedKeyword);
			if (existingSuggestions.Any())
			{
				_memoryCache.Set(cacheKey, existingSuggestions, TimeSpan.FromHours(24));
				return existingSuggestions;
			}

			var httpClient = await _apiServiceFactory.CreateRapidApiClientAsync("ahrefs-keyword-tool.p.rapidapi.com");
			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri($"https://ahrefs-keyword-tool.p.rapidapi.com/global-volume?keyword={Uri.EscapeDataString(seedKeyword)}"),
			};

			using (var response = await httpClient.SendAsync(request))
			{
				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && errorContent.Contains("You are not subscribed to this API"))
					{
						throw new Exception("Tài khoản RapidAPI của bạn chưa đăng ký API ahrefs-keyword-tool.p.rapidapi.com. Vui lòng đăng ký trên RapidAPI.");
					}
					throw new Exception($"Không thể truy cập API: {response.StatusCode} ({response.ReasonPhrase}). Chi tiết: {errorContent}");
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				_logger.LogDebug("API Response for seed keyword {SeedKeyword}: {JsonResponse}", seedKeyword, jsonResponse);
				var keywordData = JsonConvert.DeserializeObject<AhrefsKeywordResponse>(jsonResponse);

				var seedKeywordEntity = new SeedKeyword
				{
					ProjectID = projectId,
					Keyword = seedKeyword,
					CreatedDate = DateTime.UtcNow,
					CompetitionValue = "N/A"
				};

				if (keywordData.GlobalKeywordData != null && keywordData.GlobalKeywordData.Length > 0)
				{
					var mainKeyword = keywordData.GlobalKeywordData[0];
					UpdateKeywordWithData(seedKeywordEntity, mainKeyword);
					var mainRelatedKeyword = CreateRelatedKeyword(seedKeywordEntity, mainKeyword);
					seedKeywordEntity.RelatedKeywords.Add(mainRelatedKeyword);
				}
				else
				{
					_logger.LogWarning("No global keyword data found for seed keyword: {SeedKeyword}", seedKeyword);
				}

				if (keywordData.RelatedKeywordDataGlobal != null)
				{
					var topRelatedKeywords = keywordData.RelatedKeywordDataGlobal
						.OrderByDescending(k => k.avg_monthly_searches)
						.Take(10)
						.ToArray();
					foreach (var related in topRelatedKeywords)
					{
						var relatedKeyword = CreateRelatedKeyword(seedKeywordEntity, related);
						seedKeywordEntity.RelatedKeywords.Add(relatedKeyword);
					}
				}
				_logger.LogDebug("Saving SeedKeyword: ProjectID={ProjectID}, Keyword={Keyword}, SearchVolume={SearchVolume}, Difficulty={Difficulty}, CPC={CPC}, CompetitionValue={CompetitionValue}, MonthlySearchVolumesJson={MonthlySearchVolumesJson}",
			seedKeywordEntity.ProjectID, seedKeywordEntity.Keyword, seedKeywordEntity.SearchVolume, seedKeywordEntity.Difficulty, seedKeywordEntity.CPC, seedKeywordEntity.CompetitionValue, seedKeywordEntity.MonthlySearchVolumesJson);
				await _keywordSuggestionRepository.AddAsync(seedKeywordEntity);

				var result = new List<SeedKeyword> { seedKeywordEntity };
				_memoryCache.Set(cacheKey, result, TimeSpan.FromHours(24));
				_logger.LogInformation("Cached and returned {Count} seed keywords for project {ProjectId} and seed {SeedKeyword}", result.Count, projectId, seedKeyword);
				return result;
			}
		}

		private void UpdateKeywordWithData(KeywordBase keyword, KeywordIdea keywordIdea)
		{
			if (keywordIdea == null) throw new ArgumentNullException(nameof(keywordIdea));

			decimal cpc = 0m;
			try
			{
				cpc = (decimal.Parse(keywordIdea.High_CPC.Replace("$", "")) + decimal.Parse(keywordIdea.Low_CPC.Replace("$", ""))) / 2;
			}
			catch (FormatException ex)
			{
				_logger.LogWarning(ex, "Failed to parse CPC for keyword {Keyword}. Setting CPC to 0.", keywordIdea.keyword);
			}

			keyword.SearchVolume = keywordIdea.avg_monthly_searches;
			keyword.Difficulty = keywordIdea.competition_index;
			keyword.CPC = cpc > 0 ? cpc : 0m;
			keyword.CompetitionValue = keywordIdea.competition_value ?? "N/A";
			keyword.MonthlySearchVolumes = keywordIdea.monthly_search_volumes?.Select(m => new MonthlyVolume
			{
				Month = m.Month,
				Year = m.Year,
				Searches = m.Searches
			}).ToList() ?? new List<MonthlyVolume>();
			_logger.LogDebug("Updated KeywordBase: SearchVolume={SearchVolume}, Difficulty={Difficulty}, CPC={CPC}, CompetitionValue={CompetitionValue}, MonthlySearchVolumesJson={MonthlySearchVolumesJson}",
		keyword.SearchVolume, keyword.Difficulty, keyword.CPC, keyword.CompetitionValue, keyword.MonthlySearchVolumesJson);
		}

		private RelatedKeyword CreateRelatedKeyword(SeedKeyword seedKeyword, KeywordIdea keywordIdea)
		{
			if (keywordIdea == null) throw new ArgumentNullException(nameof(keywordIdea));

			var relatedKeyword = new RelatedKeyword
			{
				SeedKeywordId = seedKeyword.Id,
				SuggestedKeyword = keywordIdea.keyword,
				CreatedDate = DateTime.UtcNow
			};

			UpdateKeywordWithData(relatedKeyword, keywordIdea);
			return relatedKeyword;
		}

		public async Task UpdateAsync(SeedKeyword entity)
		{
			await _keywordSuggestionRepository.UpdateAsync(entity);
		}

		private class AhrefsKeywordResponse
		{
			[JsonProperty("Global Keyword Data")] // Sử dụng JsonPropertyName
			public KeywordIdea[] GlobalKeywordData { get; set; }

			[JsonProperty("Related Keyword Data (Global)")] // Sử dụng JsonPropertyName
			public KeywordIdea[] RelatedKeywordDataGlobal { get; set; }
		}

		private class KeywordIdea
		{
			[JsonProperty("keyword")] // Sử dụng JsonPropertyName
			public string keyword { get; set; }

			[JsonProperty("avg_monthly_searches")] // Sử dụng JsonPropertyName
			public int avg_monthly_searches { get; set; }

			[JsonProperty("competition_index")] // Sử dụng JsonPropertyName
			public int competition_index { get; set; }

			[JsonProperty("competition_value")] // Sử dụng JsonPropertyName
			public string competition_value { get; set; }

			[JsonProperty("High CPC")] // Sử dụng JsonPropertyName
			public string High_CPC { get; set; }

			[JsonProperty("Low CPC")] // Sử dụng JsonPropertyName
			public string Low_CPC { get; set; }

			[JsonProperty("monthly_search_volumes")] // Sử dụng JsonPropertyName
			public MonthlyVolume[] monthly_search_volumes { get; set; }
		}
	}
}

