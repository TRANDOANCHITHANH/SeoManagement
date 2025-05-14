using System.Text.Json.Serialization;

namespace SeoManagement.Infrastructure.Services
{
	public class PageSpeedService
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;

		public PageSpeedService(HttpClient httpClient, string apiKey)
		{
			_httpClient = httpClient;
			_apiKey = apiKey;
		}

		public async Task<(double LoadTime, double? LCP, double? FID, double? CLS, string Suggestions)> CheckPageSpeedAsync(string url)
		{
			try
			{
				var requestUrl = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&key={_apiKey}&strategy=desktop&category=performance";
				var response = await _httpClient.GetAsync(requestUrl);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var result = System.Text.Json.JsonSerializer.Deserialize<PageSpeedResponse>(json);
				if (result == null)
				{
					throw new Exception("Không thể phân tích dữ liệu từ API.");
				}

				double loadTime = 0;
				if (result.OriginLoadingExperience?.Metrics != null && result.OriginLoadingExperience.Metrics.TryGetValue("FIRST_CONTENTFUL_PAINT_MS", out var fcpMetric) && fcpMetric?.Percentile != null)
				{
					loadTime = fcpMetric.Percentile.Value / 1000.0; // FCP từ originLoadingExperience

				}
				else if (result.LighthouseResult?.Audits?["speed-index"]?.NumericValue != null)
				{
					loadTime = (double)(result.LighthouseResult.Audits["speed-index"].NumericValue / 1000.0); // Fallback sang speed-index

				}
				else if (result.LighthouseResult?.Audits?["first-contentful-paint"]?.NumericValue != null)
				{
					loadTime = (double)(result.LighthouseResult.Audits["first-contentful-paint"].NumericValue / 1000.0);

				}

				double? lcp = null;
				if (result.OriginLoadingExperience?.Metrics != null && result.OriginLoadingExperience.Metrics.TryGetValue("LARGEST_CONTENTFUL_PAINT_MS", out var lcpMetric) && lcpMetric?.Percentile != null)
				{
					lcp = lcpMetric.Percentile.Value / 1000.0; // LCP từ originLoadingExperience
				}
				else if (result.LighthouseResult?.Audits?["largest-contentful-paint"]?.NumericValue != null)
				{
					lcp = result.LighthouseResult.Audits["largest-contentful-paint"].NumericValue / 1000.0; // Fallback sang LCP từ Lighthouse
				}

				// Lấy FID
				double? fid = null;
				if (result.LighthouseResult?.Audits?["max-potential-fid"]?.NumericValue != null)
				{
					fid = result.LighthouseResult.Audits["max-potential-fid"].NumericValue;
				}

				// Lấy CLS
				double? cls = null;
				if (result.OriginLoadingExperience?.Metrics != null && result.OriginLoadingExperience.Metrics.TryGetValue("CUMULATIVE_LAYOUT_SHIFT_SCORE", out var clsMetric) && clsMetric?.Percentile != null)
				{
					cls = clsMetric.Percentile.Value / 100.0;
				}
				else if (result.LighthouseResult?.Audits?["cumulative-layout-shift"]?.NumericValue != null)
				{
					cls = result.LighthouseResult.Audits["cumulative-layout-shift"].NumericValue;
				}
				string suggestions = result.LighthouseResult?.Audits?["diagnostics"]?.Details?.ToString() ?? "Không có gợi ý.";

				return (loadTime, lcp, fid, cls, suggestions);
			}
			catch (HttpRequestException ex)
			{

				throw new Exception($"Không thể truy cập API: {ex.Message}");
			}
			catch (Exception ex)
			{

				throw;
			}
		}
	}

	public class PageSpeedResponse
	{
		[JsonPropertyName("captchaResult")]
		public string CaptchaResult { get; set; }

		[JsonPropertyName("kind")]
		public string Kind { get; set; }

		[JsonPropertyName("id")]
		public string Id { get; set; }
		[JsonPropertyName("loadingExperience")]
		public LoadingExperience LoadingExperience { get; set; }
		[JsonPropertyName("originLoadingExperience")]
		public LoadingExperience OriginLoadingExperience { get; set; }
		[JsonPropertyName("lighthouseResult")]
		public LighthouseResult LighthouseResult { get; set; }
	}

	public class LoadingExperience
	{
		[JsonPropertyName("initial_url")]
		public string InitialUrl { get; set; }

		[JsonPropertyName("metrics")]
		public Dictionary<string, Metric> Metrics { get; set; }
	}

	public class Metric
	{
		[JsonPropertyName("percentile")]
		public double? Percentile { get; set; }

		[JsonPropertyName("distributions")]
		public List<Distribution> Distributions { get; set; }

		[JsonPropertyName("category")]
		public string Category { get; set; }
	}

	public class LighthouseResult
	{
		[JsonPropertyName("requestedUrl")]
		public string RequestedUrl { get; set; }

		[JsonPropertyName("finalUrl")]
		public string FinalUrl { get; set; }

		[JsonPropertyName("audits")]
		public Dictionary<string, Audit> Audits { get; set; }
	}

	public class Distribution
	{
		[JsonPropertyName("min")]
		public double Min { get; set; }

		[JsonPropertyName("max")]
		public double? Max { get; set; }

		[JsonPropertyName("proportion")]
		public double Proportion { get; set; }
	}

	public class Audit
	{
		[JsonPropertyName("score")]
		public double? Score { get; set; }

		[JsonPropertyName("scoreDisplayMode")]
		public string ScoreDisplayMode { get; set; }

		[JsonPropertyName("displayValue")]
		public string DisplayValue { get; set; }

		[JsonPropertyName("numericValue")]
		public double? NumericValue { get; set; }

		[JsonPropertyName("numericUnit")]
		public string NumericUnit { get; set; }

		[JsonPropertyName("details")]
		public object Details { get; set; }
	}
}
