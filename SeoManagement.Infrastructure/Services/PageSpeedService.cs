using Microsoft.Extensions.Logging;
using SeoManagement.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeoManagement.Infrastructure.Services
{
	public class PageSpeedService
	{
		private readonly IApiServiceFactory _apiServiceFactory;
		private readonly ILogger<PageSpeedService> _logger;
		public PageSpeedService(IApiServiceFactory apiServiceFactory, ILogger<PageSpeedService> logger)
		{
			_apiServiceFactory = apiServiceFactory;
			_logger = logger;
		}

		public async Task<(double LoadTime, double? LCP, double? FID, double? CLS, string Suggestions)> CheckPageSpeedAsync(string url)
		{
			try
			{
				var (_httpClient, _apiKey) = await _apiServiceFactory.CreatePageSpeedClientAsync();
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
					loadTime = fcpMetric.Percentile.Value / 1000.0;
				}
				else if (result.LighthouseResult?.Audits?["speed-index"]?.NumericValue != null)
				{
					loadTime = (double)(result.LighthouseResult.Audits["speed-index"].NumericValue / 1000.0);
				}
				else if (result.LighthouseResult?.Audits?["first-contentful-paint"]?.NumericValue != null)
				{
					loadTime = (double)(result.LighthouseResult.Audits["first-contentful-paint"].NumericValue / 1000.0);
				}

				double? lcp = null;
				if (result.OriginLoadingExperience?.Metrics != null && result.OriginLoadingExperience.Metrics.TryGetValue("LARGEST_CONTENTFUL_PAINT_MS", out var lcpMetric) && lcpMetric?.Percentile != null)
				{
					lcp = lcpMetric.Percentile.Value / 1000.0;
				}
				else if (result.LighthouseResult?.Audits?["largest-contentful-paint"]?.NumericValue != null)
				{
					lcp = result.LighthouseResult.Audits["largest-contentful-paint"].NumericValue / 1000.0;
				}

				double? fid = null;
				if (result.LighthouseResult?.Audits?["max-potential-fid"]?.NumericValue != null)
				{
					fid = result.LighthouseResult.Audits["max-potential-fid"].NumericValue;
				}

				double? cls = null;
				if (result.OriginLoadingExperience?.Metrics != null && result.OriginLoadingExperience.Metrics.TryGetValue("CUMULATIVE_LAYOUT_SHIFT_SCORE", out var clsMetric) && clsMetric?.Percentile != null)
				{
					cls = clsMetric.Percentile.Value / 100.0;
				}
				else if (result.LighthouseResult?.Audits?["cumulative-layout-shift"]?.NumericValue != null)
				{
					cls = result.LighthouseResult.Audits["cumulative-layout-shift"].NumericValue;
				}

				// Phân tích chi tiết và tạo gợi ý cải thiện
				var suggestions = new List<string>();

				// Phân tích Load Time
				if (loadTime > 5)
					suggestions.Add("Thời gian tải trang quá chậm (>5s). Hãy tối ưu hình ảnh, giảm tài nguyên chặn render (CSS/JS), và sử dụng server nhanh hơn.");
				else if (loadTime > 3)
					suggestions.Add("Thời gian tải trang trung bình (3-5s). Cân nhắc nén hình ảnh, sử dụng lazy-loading cho nội dung bên dưới.");

				// Phân tích LCP (theo W3C LCP và Web Vitals)
				if (lcp.HasValue)
				{
					if (lcp > 4)
						suggestions.Add("LCP quá cao (>4s). Kiểm tra tài nguyên lớn (hình ảnh, video) hoặc nội dung chặn render. Sử dụng <img loading=\"lazy\"> và nén hình ảnh.");
					else if (lcp > 2.5)
						suggestions.Add("LCP cần cải thiện (2.5-4s). Tối ưu tải nội dung lớn ban đầu, giảm thời gian server response, hoặc sử dụng CDN.");
				}

				// Phân tích FID (theo Web Vitals)
				if (fid.HasValue)
				{
					if (fid > 300)
						suggestions.Add("FID quá cao (>300ms). Tối ưu JavaScript (deferred loading), giảm công việc nền, hoặc sử dụng Web Workers.");
					else if (fid > 100)
						suggestions.Add("FID cần cải thiện (100-300ms). Giảm thời gian xử lý đầu vào bằng cách tối ưu sự kiện click/touch.");
				}

				// Phân tích CLS (theo Web Vitals)
				if (cls.HasValue)
				{
					if (cls > 0.25)
						suggestions.Add("CLS quá cao (>0.25). Cố định kích thước hình ảnh/video, tránh nội dung động (ads, popups) không kiểm soát.");
					else if (cls > 0.1)
						suggestions.Add("CLS cần cải thiện (0.1-0.25). Đặt kích thước (width/height) cho các phần tử, đặc biệt là hình ảnh.");
				}

				// Phân tích dữ liệu diagnostics từ Lighthouse
				if (result.LighthouseResult?.Audits?.TryGetValue("diagnostics", out var diagnosticsAudit) == true)
				{
					_logger.LogInformation("Diagnostics audit found: {@Diagnostics}", diagnosticsAudit);
					if (diagnosticsAudit?.Details is JsonElement detailsElement)
					{
						try
						{
							var details = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(detailsElement.GetRawText());
							if (details?.TryGetValue("items", out var items) == true && items is JsonElement itemsElement)
							{
								var diagnosticItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(itemsElement.GetRawText());
								var firstItem = diagnosticItems?.FirstOrDefault();
								if (firstItem != null)
								{
									// Kích thước trang
									if (firstItem.TryGetValue("totalByteWeight", out var totalByteWeight) && totalByteWeight is JsonElement byteWeightElement && byteWeightElement.TryGetDouble(out double byteWeight) && byteWeight > 1000000)
										suggestions.Add("Kích thước trang quá lớn (>1MB). Nén hình ảnh, minify CSS/JS, và loại bỏ tài nguyên không cần thiết.");

									// Số lượng script
									if (firstItem.TryGetValue("numScripts", out var numScripts) && numScripts is JsonElement scriptsElement && scriptsElement.TryGetDouble(out double scripts) && scripts > 10)
										suggestions.Add("Số lượng script quá nhiều (>10). Kết hợp file JS, sử dụng defer/async attributes.");

									// Số lượng stylesheet
									if (firstItem.TryGetValue("numStylesheets", out var numStylesheets) && numStylesheets is JsonElement stylesheetsElement && stylesheetsElement.TryGetDouble(out double stylesheets) && stylesheets > 10)
										suggestions.Add("Số lượng stylesheet quá nhiều (>10). Kết hợp file CSS và giảm quy tắc không cần thiết.");

									// Số lượng font
									if (firstItem.TryGetValue("numFonts", out var numFonts) && numFonts is JsonElement fontsElement && fontsElement.TryGetDouble(out double fonts) && fonts > 5)
										suggestions.Add("Số lượng font quá nhiều (>5). Giảm font, sử dụng font-display: swap, hoặc tải trước font cần thiết.");

									// Thời gian xử lý nhiệm vụ
									if (firstItem.TryGetValue("totalTaskTime", out var totalTaskTime) && totalTaskTime is JsonElement taskTimeElement && taskTimeElement.TryGetDouble(out double taskTime) && taskTime > 500)
										suggestions.Add("Thời gian xử lý nhiệm vụ quá dài (>500ms). Tối ưu JavaScript, giảm DOM size, hoặc sử dụng code splitting.");
								}
								else
								{
									_logger.LogWarning("No items found in diagnostics details for URL: {Url}", url);
								}
							}
							else
							{
								_logger.LogWarning("No items or invalid format in diagnostics details for URL: {Url}", url);
							}
						}
						catch (JsonException ex)
						{
							_logger.LogError(ex, "Error deserializing diagnostics details for URL: {Url}", url);
						}
					}
					else
					{
						_logger.LogWarning("Diagnostics Details is not a JsonElement for URL: {Url}", url);
					}
				}

				// Render-blocking resources
				if (result.LighthouseResult?.Audits?.TryGetValue("render-blocking-resources", out var renderBlocking) == true && renderBlocking?.Score < 0.9)
					suggestions.Add("Tài nguyên chặn render (CSS/JS) được phát hiện. Inline critical CSS hoặc defer non-critical JS.");

				// Offscreen images
				if (result.LighthouseResult?.Audits?.TryGetValue("offscreen-images", out var offscreenImages) == true && offscreenImages?.Score < 0.9)
					suggestions.Add("Hình ảnh ngoài màn hình (offscreen) được tải. Sử dụng lazy-loading (<img loading=\"lazy\">).");

				string suggestionsText = suggestions.Any() ? string.Join("\n", suggestions) : "Hiệu suất trang tốt. Không có gợi ý cải thiện cụ thể.";
				return (loadTime, lcp, fid, cls, suggestionsText);
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
