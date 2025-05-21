using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

		public async Task AddAsync(KeywordSuggestion entity)
		{
			await _keywordSuggestionRepository.AddAsync(entity);
		}

		public async Task DeleteAsync(int id)
		{
			await _keywordSuggestionRepository.DeleteAsync(id);
		}

		public async Task<List<KeywordSuggestion>> GetByProjectIdAsync(int id)
		{
			return await _keywordSuggestionRepository.GetByProjectIdAsync(id);
		}

		public async Task<List<KeywordSuggestion>> ResearchKeywordsAsync(int projectId, string seedKeyword)
		{
			var cacheKey = $"KeywordResearch_{projectId}_{seedKeyword}";
			if (_memoryCache.TryGetValue(cacheKey, out List<KeywordSuggestion> cachedSuggestions))
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
				var keywordData = JsonConvert.DeserializeObject<AhrefsKeywordResponse>(jsonResponse);
				_logger.LogInformation("JSON Response: {Json}", jsonResponse);
				_logger.LogInformation("Keyword Data: {@KeywordData}", keywordData);
				var suggestions = new List<KeywordSuggestion>();

				// Thêm từ khóa chính
				if (keywordData.GlobalKeywordData != null && keywordData.GlobalKeywordData.Length > 0)
				{
					var mainKeyword = keywordData.GlobalKeywordData[0];
					var suggestion = CreateKeywordSuggestion(projectId, seedKeyword, mainKeyword, true);
					var existingSuggestion = existingSuggestions.FirstOrDefault(s => s.SeedKeyword == seedKeyword && s.SuggestedKeyword == mainKeyword.keyword);
					if (existingSuggestion != null)
					{
						UpdateExistingSuggestion(existingSuggestion, suggestion);
						await _keywordSuggestionRepository.UpdateAsync(existingSuggestion);
						suggestions.Add(existingSuggestion);
					}
					else
					{
						suggestions.Add(suggestion);
					}
				}
				else
				{
					_logger.LogWarning("No global keyword data found for seed keyword: {SeedKeyword}", seedKeyword);
				}

				// Thêm từ khóa liên quan
				if (keywordData.RelatedKeywordDataGlobal != null)
				{
					var topRelatedKeywords = keywordData.RelatedKeywordDataGlobal
						.OrderByDescending(k => k.avg_monthly_searches)
						.Take(10)
						.ToArray();
					foreach (var related in topRelatedKeywords)
					{
						var suggestion = CreateKeywordSuggestion(projectId, seedKeyword, related, false);
						var existingSuggestion = existingSuggestions.FirstOrDefault(s => s.SeedKeyword == seedKeyword && s.SuggestedKeyword == related.keyword);
						if (existingSuggestion != null)
						{
							UpdateExistingSuggestion(existingSuggestion, suggestion);
							await _keywordSuggestionRepository.UpdateAsync(existingSuggestion);
							suggestions.Add(existingSuggestion);
						}
						else
						{
							suggestions.Add(suggestion);
						}
					}
				}

				if (suggestions.Any(s => s.Id == 0))
				{
					var newSuggestions = suggestions.Where(s => s.Id == 0).ToList();
					await _keywordSuggestionRepository.AddSuggestionsAsync(newSuggestions);
					_logger.LogInformation("Added {Count} new suggestions to database", newSuggestions.Count);
				}
				_memoryCache.Set(cacheKey, suggestions, TimeSpan.FromHours(24));
				_logger.LogInformation("Cached and returned {Count} suggestions for project {ProjectId} and seed {SeedKeyword}", suggestions.Count, projectId, seedKeyword);
				return suggestions;
			}
		}

		private KeywordSuggestion CreateKeywordSuggestion(int projectId, string seedKeyword, KeywordIdea keywordIdea, bool isMainKeyword)
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

			return new KeywordSuggestion
			{
				ProjectID = projectId,
				SeedKeyword = seedKeyword,
				SuggestedKeyword = keywordIdea.keyword,
				IsMainKeyword = isMainKeyword,
				SearchVolume = keywordIdea.avg_monthly_searches,
				Difficulty = keywordIdea.competition_index,
				CPC = cpc > 0 ? cpc : 0m,
				MonthlySearchVolumes = keywordIdea.monthly_search_volumes?.Select(m => new MonthlySearchVolume
				{
					Month = m.month,
					Year = m.year,
					Searches = m.searches
				}).ToList() ?? new List<MonthlySearchVolume>(),
				CreatedDate = DateTime.UtcNow
			};
		}

		private void UpdateExistingSuggestion(KeywordSuggestion existing, KeywordSuggestion newData)
		{
			existing.SearchVolume = newData.SearchVolume;
			existing.Difficulty = newData.Difficulty;
			existing.CPC = newData.CPC;
			existing.CreatedDate = newData.CreatedDate;
			existing.IsMainKeyword = newData.IsMainKeyword;

			var existingVolumes = existing.MonthlySearchVolumes.ToList();
			var newVolumes = newData.MonthlySearchVolumes.ToList();
			var volumesToRemove = existingVolumes.Where(ev => !newVolumes.Any(nv => nv.Month == ev.Month && nv.Year == ev.Year)).ToList();
			foreach (var volume in volumesToRemove)
			{
				existing.MonthlySearchVolumes.Remove(volume);
			}

			foreach (var newVolume in newVolumes)
			{
				var existingVolume = existing.MonthlySearchVolumes.FirstOrDefault(ev => ev.Month == newVolume.Month && ev.Year == newVolume.Year);
				if (existingVolume != null)
				{
					existingVolume.Searches = newVolume.Searches;
				}
				else
				{
					existing.MonthlySearchVolumes.Add(newVolume);
				}
			}
		}

		public async Task UpdateAsync(KeywordSuggestion entity)
		{
			await _keywordSuggestionRepository.UpdateAsync(entity);
		}

		private class AhrefsKeywordResponse
		{
			[JsonProperty("Global Keyword Data")]
			public KeywordIdea[] GlobalKeywordData { get; set; }

			[JsonProperty("Related Keyword Data (Global)")]
			public KeywordIdea[] RelatedKeywordDataGlobal { get; set; }
		}

		private class KeywordIdea
		{
			[JsonProperty("keyword")]
			public string keyword { get; set; }

			[JsonProperty("avg_monthly_searches")]
			public int avg_monthly_searches { get; set; }

			[JsonProperty("competition_index")]
			public int competition_index { get; set; }

			[JsonProperty("competition_value")]
			public string competition_value { get; set; }

			[JsonProperty("High CPC")]
			public string High_CPC { get; set; }

			[JsonProperty("Low CPC")]
			public string Low_CPC { get; set; }

			[JsonProperty("monthly_search_volumes")]
			public MonthlyVolume[] monthly_search_volumes { get; set; }
		}

		private class MonthlyVolume
		{
			[JsonProperty("month")]
			public string month { get; set; }

			[JsonProperty("searches")]
			public int searches { get; set; }

			[JsonProperty("year")]
			public int year { get; set; }
		}
	}
}
