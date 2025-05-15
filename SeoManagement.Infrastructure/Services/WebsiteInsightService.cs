using Microsoft.Extensions.Configuration;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using System.Text.Json;

namespace SeoManagement.Infrastructure.Services
{
	public class WebsiteInsightService : IService<WebsiteInsight>
	{
		private readonly IWebsiteInsightRepository _repository;
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;

		public WebsiteInsightService(IWebsiteInsightRepository repository, HttpClient httpClient, IConfiguration configuration)
		{
			_repository = repository;
			_httpClient = httpClient;
			_apiKey = configuration["RapidApiKey"];
			_httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "similarweb-insights.p.rapidapi.com");
			_httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", _apiKey);
		}

		public async Task AddAsync(WebsiteInsight entity)
		{
			await _repository.AddAsync(entity);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
		}

		public async Task<List<WebsiteInsight>> GetByProjectIdAsync(int id)
		{
			return await _repository.GetByProjectIdAsync(id);
		}

		public async Task UpdateAsync(WebsiteInsight entity)
		{
			await _repository.UpdateAsync(entity);
		}

		public async Task<WebsiteInsight> GetAndSaveWebsiteInsightsAsync(string domain, int projectId)
		{
			if (string.IsNullOrWhiteSpace(domain))
			{
				throw new ArgumentException("Domain cannot be empty.", nameof(domain));
			}

			if (!domain.StartsWith("http://") && !domain.StartsWith("https://"))
			{
				domain = "https://" + domain;
			}

			var requestUri = $"https://similarweb-insights.p.rapidapi.com/all-insights?domain={Uri.EscapeDataString(domain)}";
			var response = await _httpClient.GetAsync(requestUri);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"API Response for {domain}: {json}");
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			var websiteInsight = new WebsiteInsight
			{
				ProjectID = projectId,
				Domain = root.TryGetProperty("WebsiteDetails", out var websiteDetails) && websiteDetails.TryGetProperty("Domain", out var domainProp) ? domainProp.GetString() : domain,
				Title = websiteDetails.TryGetProperty("Title", out var titleProp) ? titleProp.GetString() : null,
				Description = websiteDetails.TryGetProperty("Description", out var descProp) ? descProp.GetString() : null,
				Category = websiteDetails.TryGetProperty("Category", out var catProp) ? catProp.GetString() : null,
				SnapshotDate = root.TryGetProperty("SnapshotDate", out var snapshotProp) && DateTime.TryParse(snapshotProp.GetString(), out var snapshotDate) ? snapshotDate : DateTime.UtcNow,
				GlobalVisits = root.TryGetProperty("Traffic", out var traffic) && traffic.TryGetProperty("Visits", out var visits) && visits.EnumerateObject().Any() && visits.EnumerateObject().Last().Value.ValueKind == JsonValueKind.Number ? visits.EnumerateObject().Last().Value.GetInt64() : (long?)null,
				BounceRate = traffic.TryGetProperty("Engagement", out var engagement) && engagement.TryGetProperty("BounceRate", out var bounceProp) && bounceProp.ValueKind == JsonValueKind.Number ? bounceProp.GetDouble() : (double?)null,
				PagesPerVisit = engagement.TryGetProperty("PagesPerVisit", out var pagesProp) && pagesProp.ValueKind == JsonValueKind.Number ? pagesProp.GetDouble() : (double?)null,
				TimeOnSite = engagement.TryGetProperty("TimeOnSite", out var timeProp) && timeProp.ValueKind == JsonValueKind.Number ? timeProp.GetDouble() : (double?)null,
				SearchTrafficPercentage = traffic.TryGetProperty("Sources", out var sources) && sources.TryGetProperty("Search", out var searchProp) && searchProp.ValueKind == JsonValueKind.Number ? searchProp.GetDouble() : (double?)null,
				DirectTrafficPercentage = sources.TryGetProperty("Direct", out var directProp) && directProp.ValueKind == JsonValueKind.Number ? directProp.GetDouble() : (double?)null,
				ReferralTrafficPercentage = sources.TryGetProperty("Referrals", out var referralsProp) && referralsProp.ValueKind == JsonValueKind.Number ? referralsProp.GetDouble() : (double?)null,
				SocialTrafficPercentage = sources.TryGetProperty("Social", out var socialProp) && socialProp.ValueKind == JsonValueKind.Number ? socialProp.GetDouble() : (double?)null,
				PaidReferralTrafficPercentage = sources.TryGetProperty("Paid Referrals", out var paidProp) && paidProp.ValueKind == JsonValueKind.Number ? paidProp.GetDouble() : (double?)null,
				MailTrafficPercentage = sources.TryGetProperty("Mail", out var mailProp) && mailProp.ValueKind == JsonValueKind.Number ? mailProp.GetDouble() : (double?)null,
				TopCountrySharesJson = traffic.TryGetProperty("TopCountryShares", out var topCountryShares) && topCountryShares.ValueKind == JsonValueKind.Object
				   ? topCountryShares.ToString()
				   : "{}",
				IsDataFromGa = traffic.TryGetProperty("IsDataFromGa", out var isDataFromGa) && isDataFromGa.ValueKind == JsonValueKind.True ? true : (isDataFromGa.ValueKind == JsonValueKind.False ? false : (bool?)null),
				TopKeywordsJson = root.TryGetProperty("SEOInsights", out var seoInsights) && seoInsights.TryGetProperty("TopKeywords", out var topKeywords) && topKeywords.ValueKind == JsonValueKind.Array
				   ? JsonSerializer.Serialize(topKeywords.EnumerateArray().Select(k => new
				   {
					   Name = k.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : null,
					   EstimatedValue = k.TryGetProperty("EstimatedValue", out var estValProp) && estValProp.ValueKind == JsonValueKind.Number ? estValProp.GetInt64() : 0,
					   Volume = k.TryGetProperty("Volume", out var volProp) && volProp.ValueKind == JsonValueKind.Number ? volProp.GetInt64() : (long?)null,
					   CPC = k.TryGetProperty("CPC", out var cpc) && cpc.ValueKind == JsonValueKind.Number ? cpc.GetDouble() : (double?)null
				   }))
				   : "[]",
				GlobalRank = root.TryGetProperty("Rank", out var rank) && rank.TryGetProperty("GlobalRank", out var globalRankProp) && globalRankProp.ValueKind == JsonValueKind.Number ? globalRankProp.GetInt64() : (long?)null,
				CountryRankCountry = rank.TryGetProperty("CountryRank", out var countryRank) && countryRank.TryGetProperty("Country", out var countryProp) ? countryProp.GetString() : null,
				CountryRankValue = countryRank.TryGetProperty("Rank", out var countryRankValProp) && countryRankValProp.ValueKind == JsonValueKind.Number ? countryRankValProp.GetInt64() : (long?)null,
				CategoryRankCategory = rank.TryGetProperty("CategoryRank", out var categoryRank) && categoryRank.TryGetProperty("Category", out var catRankProp) ? catRankProp.GetString() : null,
				CategoryRankValue = categoryRank.TryGetProperty("Rank", out var catRankValProp) && catRankValProp.ValueKind == JsonValueKind.Number ? catRankValProp.GetInt64() : (long?)null
			};

			var existingInsight = (await _repository.GetByProjectIdAsync(projectId))
				.FirstOrDefault(w => w.Domain == domain);
			if (existingInsight != null)
			{
				existingInsight.Title = websiteInsight.Title;
				existingInsight.Description = websiteInsight.Description;
				existingInsight.Category = websiteInsight.Category;
				existingInsight.SnapshotDate = websiteInsight.SnapshotDate;
				existingInsight.GlobalVisits = websiteInsight.GlobalVisits;
				existingInsight.BounceRate = websiteInsight.BounceRate;
				existingInsight.PagesPerVisit = websiteInsight.PagesPerVisit;
				existingInsight.TimeOnSite = websiteInsight.TimeOnSite;
				existingInsight.SearchTrafficPercentage = websiteInsight.SearchTrafficPercentage;
				existingInsight.DirectTrafficPercentage = websiteInsight.DirectTrafficPercentage;
				existingInsight.ReferralTrafficPercentage = websiteInsight.ReferralTrafficPercentage;
				existingInsight.SocialTrafficPercentage = websiteInsight.SocialTrafficPercentage;
				existingInsight.PaidReferralTrafficPercentage = websiteInsight.PaidReferralTrafficPercentage;
				existingInsight.MailTrafficPercentage = websiteInsight.MailTrafficPercentage;
				existingInsight.TopCountrySharesJson = websiteInsight.TopCountrySharesJson;
				existingInsight.IsDataFromGa = websiteInsight.IsDataFromGa;
				existingInsight.TopKeywordsJson = websiteInsight.TopKeywordsJson;
				existingInsight.GlobalRank = websiteInsight.GlobalRank;
				existingInsight.CountryRankCountry = websiteInsight.CountryRankCountry;
				existingInsight.CountryRankValue = websiteInsight.CountryRankValue;
				existingInsight.CategoryRankCategory = websiteInsight.CategoryRankCategory;
				existingInsight.CategoryRankValue = websiteInsight.CategoryRankValue;
				await _repository.UpdateAsync(existingInsight);
				return existingInsight;
			}


			await _repository.AddAsync(websiteInsight);
			return websiteInsight;
		}
	}
}
