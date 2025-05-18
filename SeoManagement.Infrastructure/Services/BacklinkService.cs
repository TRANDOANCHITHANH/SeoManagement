using SeoManagement.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeoManagement.Infrastructure.Services
{
	public class BacklinkService
	{
		private readonly IApiServiceFactory _apiServiceFactory;

		public BacklinkService(IApiServiceFactory apiServiceFactory)
		{
			_apiServiceFactory = apiServiceFactory;
		}

		public async Task<(int TotalBacklinks, int ReferringDomains, int DofollowBacklinks, int DofollowRefDomains, string BacklinksDetails)> CheckBacklinksAsync(string url)
		{
			try
			{
				var _httpClient = await _apiServiceFactory.CreateRapidApiClientAsync("ahrefs2.p.rapidapi.com");
				var request = new HttpRequestMessage
				{
					Method = HttpMethod.Get,
					RequestUri = new Uri($"https://ahrefs2.p.rapidapi.com/backlinks?url={Uri.EscapeDataString(url)}&mode=subdomains"),
				};

				using var response = await _httpClient.SendAsync(request);
				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && errorContent.Contains("You are not subscribed to this API"))
					{
						throw new Exception("Tài khoản RapidAPI của bạn chưa đăng ký API ahrefs2.p.rapidapi.com. Vui lòng đăng ký trên RapidAPI.");
					}
					throw new Exception($"Không thể truy cập API: {response.StatusCode} ({response.ReasonPhrase}). Chi tiết: {errorContent}");
				}

				var json = await response.Content.ReadAsStringAsync();
				if (string.IsNullOrWhiteSpace(json))
				{
					throw new Exception("Dữ liệu trả về từ API trống.");
				}

				var backlinkData = JsonSerializer.Deserialize<BacklinkResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (backlinkData == null)
				{
					throw new Exception("Không thể phân tích dữ liệu từ API. Dữ liệu không đúng định dạng mong đợi.");
				}

				int totalBacklinks = backlinkData.Backlinks ?? 0;
				int referringDomains = backlinkData.RefDomains ?? 0;
				int dofollowBacklinks = backlinkData.DofollowBacklinks ?? 0;
				int dofollowRefDomains = backlinkData.DofollowRefDomains ?? 0;
				string backlinksDetails = backlinkData.BacklinksList != null ? JsonSerializer.Serialize(backlinkData.BacklinksList) : "[]";

				return (totalBacklinks, referringDomains, dofollowBacklinks, dofollowRefDomains, backlinksDetails);
			}
			catch (JsonException ex)
			{
				throw new Exception($"Lỗi phân tích JSON từ API: {ex.Message}", ex);
			}
			catch (HttpRequestException ex)
			{
				throw new Exception($"Không thể truy cập API: {ex.Message}", ex);
			}
			catch (Exception ex)
			{
				throw new Exception($"Lỗi không xác định: {ex.Message}", ex);
			}
		}
	}

	public class BacklinkResponse
	{
		[JsonPropertyName("status")]
		public string Status { get; set; }

		[JsonPropertyName("url")]
		public string Url { get; set; }

		[JsonPropertyName("domainRating")]
		public int DomainRating { get; set; }

		[JsonPropertyName("urlRating")]
		public int UrlRating { get; set; }

		[JsonPropertyName("backlinks")]
		public int? Backlinks { get; set; }

		[JsonPropertyName("refdomains")]
		public int? RefDomains { get; set; }

		[JsonPropertyName("dofollowBacklinks")]
		public int? DofollowBacklinks { get; set; }

		[JsonPropertyName("dofollowRefdomains")]
		public int? DofollowRefDomains { get; set; }

		[JsonPropertyName("backlinksList")]
		public BacklinkResult[] BacklinksList { get; set; }
	}

	public class BacklinkResult
	{
		[JsonPropertyName("url")]
		public string FromUrl { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("domain_rating")]
		public int DomainRating { get; set; }

		[JsonPropertyName("target_url")]
		public string TargetUrl { get; set; }

		[JsonPropertyName("anchor")]
		public string Anchor { get; set; }

		[JsonPropertyName("summary")]
		public string Summary { get; set; }

		[JsonPropertyName("link")]
		public string Link { get; set; }
	}
}