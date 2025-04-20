using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SeoManagement.Infrastructure.Services
{
	public class GoogleSearchConsoleService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<GoogleSearchConsoleService> _logger;

		public GoogleSearchConsoleService(IConfiguration configuration, ILogger<GoogleSearchConsoleService> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		//public async Task<List<(string Keyword, double Clicks, double Impressions, double Ctr, double Position)>> GetSeoDataAsync(string siteUrl, DateTime startDate, DateTime endDate)
		//{
		//	try
		//	{
		//		_logger.LogInformation("Initializing Google Search Console service for site: {SiteUrl}", siteUrl);

		//		using var stream = new FileStream(_configuration["GoogleSearchConsole:CredentialsPath"], FileMode.Open, FileAccess.Read);
		//		var credential = GoogleCredential.FromStream(stream)
		//			.CreateScoped(SearchConsoleService.Scope.Webmasters);

		//		var service = new SearchConsoleService(new BaseClientService.Initializer
		//		{
		//			HttpClientInitializer = credential,
		//			ApplicationName = "SeoManagement"
		//		});

		//		var request = service.Searchanalytics.Query(new Google.Apis.SearchConsole.v1.Data.SearchAnalyticsQueryRequest
		//		{
		//			StartDate = startDate.ToString("yyyy-MM-dd"),
		//			EndDate = endDate.ToString("yyyy-MM-dd"),
		//			Dimensions = new[] { "query" },
		//			RowLimit = 100
		//		});
		//		request.SiteUrl = siteUrl;

		//		_logger.LogInformation("Fetching SEO data for {SiteUrl} from {StartDate} to {EndDate}", siteUrl, startDate, endDate);
		//		var response = await request.ExecuteAsync();

		//		var seoData = response.Rows?.Select(row => (
		//			Keyword: row.Keys.First(),
		//			Clicks: row.Clicks ?? 0,
		//			Impressions: row.Impressions ?? 0,
		//			Ctr: row.Ctr ?? 0,
		//			Position: row.Position ?? 0
		//		)).ToList() ?? new List<(string, double, double, double, double)>();

		//		_logger.LogInformation("Retrieved {Count} SEO records for {SiteUrl}", seoData.Count, siteUrl);
		//		return seoData;
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, "Failed to fetch SEO data for {SiteUrl}: {Message}", siteUrl, ex.Message);
		//		throw;
		//	}
		//}
	}
}
