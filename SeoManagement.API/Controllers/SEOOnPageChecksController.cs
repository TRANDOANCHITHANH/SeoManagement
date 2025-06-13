using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.API.Models.Dtos;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
namespace SeoManagement.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SEOOnPageChecksController : ControllerBase
	{
		private readonly ISEOOnPageCheckService _seoOnPageCheckService;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ILogger<SEOOnPageChecksController> _logger;
		private readonly string _pageSpeedApiKey;
		public SEOOnPageChecksController(ISEOOnPageCheckService seoOnPageCheckService, IHttpClientFactory httpClientFactory, ILogger<SEOOnPageChecksController> logger, IConfiguration configuration)
		{
			_seoOnPageCheckService = seoOnPageCheckService;
			_httpClientFactory = httpClientFactory;
			_logger = logger;
			_pageSpeedApiKey = configuration["GooglePageSpeed:ApiKey"];
		}

		[HttpGet("project/{projectId}")]
		public async Task<IActionResult> GetAll(int projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			try
			{
				var (checks, totalItems) = await _seoOnPageCheckService.GetPagedAsync(projectId, pageNumber, pageSize);
				var checkDtos = checks.Select(c => new
				{
					CheckID = c.CheckID,
					ProjectID = c.ProjectID,
					Url = c.Url,
					MainKeyword = c.MainKeyword,
					CreatedAt = c.CreatedAt
				}).ToList();

				var result = new
				{
					Items = checkDtos,
					TotalItems = totalItems,
					PageNumber = pageNumber,
					PageSize = pageSize
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy danh sách SEOOnPageChecks cho projectId: {ProjectId}", projectId);
				return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null)
			{
				return NotFound();
			}

			var checkDto = new SEOOnPageCheckDto
			{
				CheckID = check.CheckID,
				ProjectID = check.ProjectID,
				Url = check.Url,
				MainKeyword = check.MainKeyword,
				CreatedAt = check.CreatedAt
			};
			return Ok(checkDto);
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] SEOOnPageCheckDto checkDto)
		{
			var check = new SEOOnPageCheck
			{
				ProjectID = checkDto.ProjectID,
				Url = checkDto.Url,
				MainKeyword = checkDto.MainKeyword,
			};

			await _seoOnPageCheckService.CreateSEOOnPageCheckAsync(check);
			return CreatedAtAction(nameof(GetById), new { id = check.CheckID }, checkDto);
		}

		[HttpPut]
		public async Task<IActionResult> Update(int id, [FromBody] SEOOnPageCheckDto checkDto)
		{
			if (id != checkDto.CheckID)
				return BadRequest();

			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null)
				return NotFound();

			check.ProjectID = checkDto.ProjectID;
			check.Url = checkDto.Url;
			check.MainKeyword = checkDto.MainKeyword;

			await _seoOnPageCheckService.UpdateSEOOnPageCheckAsync(check);
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null) return NotFound();

			await _seoOnPageCheckService.DeleteSEOOnPageCheckAsync(id);
			return NoContent();
		}

		[HttpPost("{id}/analyze")]
		public async Task<IActionResult> Analyze(int id)
		{
			var check = await _seoOnPageCheckService.GetByIdAsync(id);
			if (check == null) return NotFound();

			try
			{
				var analysisResult = await AnalyzeHtml(check.Url, check.MainKeyword, _logger);
				var result = new SEOOnPageAnalysisResult
				{
					Title = analysisResult.Title,
					MetaDescription = analysisResult.MetaDescription,
					WordCount = analysisResult.WordCount,
					IsTitleLengthOptimal = !string.IsNullOrEmpty(analysisResult.Title) && analysisResult.Title.Length >= 30 && analysisResult.Title.Length <= 60,
					IsMetaDescriptionLengthOptimal = !string.IsNullOrEmpty(analysisResult.MetaDescription) && analysisResult.MetaDescription.Length >= 120 && analysisResult.MetaDescription.Length <= 160,
					IsMainKeywordInTitle = !string.IsNullOrEmpty(check.MainKeyword) && !string.IsNullOrEmpty(analysisResult.Title) && analysisResult.Title.ToLower().Contains(check.MainKeyword.ToLower()),
					IsMainKeywordInMetaDescription = !string.IsNullOrEmpty(check.MainKeyword) && !string.IsNullOrEmpty(analysisResult.MetaDescription) && analysisResult.MetaDescription.ToLower().Contains(check.MainKeyword.ToLower()),
					IsWordCountSufficient = analysisResult.WordCount >= 300,
					HeadingCount = analysisResult.HeadingCount,
					H1Count = analysisResult.H1Count,
					ImageCountWithoutAlt = analysisResult.ImageCountWithoutAlt,
					KeywordDensity = analysisResult.KeywordDensity,
					InternalLinkCount = analysisResult.InternalLinkCount,
					BrokenLinkCount = analysisResult.BrokenLinkCount,
					HasCanonicalUrl = analysisResult.HasCanonicalUrl,
					HasStructuredData = analysisResult.HasStructuredData
				};

				// Kiểm tra HTTPS
				result.IsHttps = !string.IsNullOrEmpty(check.Url) && Uri.TryCreate(check.Url, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;

				// Phân tích tốc độ trang
				result.PageSpeedScoreDesktop = await AnalyzePageSpeed(check.Url, "desktop");
				result.PageSpeedScoreMobile = await AnalyzePageSpeed(check.Url, "mobile");

				result.Summary = GenerateSummary(result);

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error analyzing SEO On-Page for CheckID: {CheckID}", id);
				return StatusCode(500, "Error analyzing SEO On-Page. Please try again later.");
			}
		}

		private string RemoveDiacritics(string text)
		{
			string[] vietnameseSigns = new string[]
			{
		"aAeEoOuUiIdDyY",
		"áàạảãâấầậẩẫăắằặẳẵ",
		"ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
		"éèẹẻẽêếềệểễ",
		"ÉÈẸẺẼÊẾỀỆỂỄ",
		"óòọỏõôốồộổỗơớờợởỡ",
		"ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
		"úùụủũưứừựửữ",
		"ÚÙỤỦŨƯỨỪỰỬỮ",
		"íìịỉĩ",
		"ÍÌỊỈĨ",
		"đ",
		"Đ",
		"ýỳỵỷỹ",
		"ÝỲỴỶỸ"
			};
			for (int i = 1; i < vietnameseSigns.Length; i++)
			{
				for (int j = 0; j < vietnameseSigns[i].Length; j++)
					text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
			}
			return text.Normalize().Trim();
		}

		private async Task<(string Title, string MetaDescription, int WordCount, int HeadingCount, int H1Count, int ImageCountWithoutAlt, double KeywordDensity, int InternalLinkCount, int BrokenLinkCount, bool HasCanonicalUrl, bool HasStructuredData)> AnalyzeHtml(string url, string mainKeyword, ILogger logger)
		{
			var httpClient = _httpClientFactory.CreateClient();
			httpClient.Timeout = TimeSpan.FromSeconds(15);
			string html;
			try
			{
				using var response = await httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();
				html = await response.Content.ReadAsStringAsync();
				html = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Convert(System.Text.Encoding.Default, System.Text.Encoding.UTF8, System.Text.Encoding.Default.GetBytes(html)));
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to fetch HTML for URL: {Url}", url);
				return (null, null, 0, 0, 0, 0, 0, 0, 0, false, false);
			}

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);

			// Trích xuất Title
			var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
			var title = titleNode?.InnerText.Trim() ?? "";

			// Trích xuất Meta Description
			var metaDescriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");
			var metaDescription = metaDescriptionNode?.GetAttributeValue("content", "").Trim() ?? "";

			// Tính WordCount từ nội dung
			var bodyTextNodes = htmlDoc.DocumentNode.SelectNodes("//body//text()[not(ancestor::script) and not(ancestor::style)] | //body//p//text() | //body//h1//text() | //body//h2//text() | //body//div//text()")
				?.Select(n => n.InnerText.Trim())
				?.Where(t => !string.IsNullOrEmpty(t));
			var bodyText = string.Join(" ", bodyTextNodes ?? new List<string>());
			var words = bodyText.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '-', '—', '–' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(w => w.Trim())
				.Where(w => !string.IsNullOrWhiteSpace(w))
				.Select(w => RemoveDiacritics(w));
			var wordCount = words.Count();

			// Phân tích Heading
			int headingCount = htmlDoc.DocumentNode.SelectNodes("//h1|//h2|//h3")
				?.Where(h => string.IsNullOrEmpty(h.GetAttributeValue("style", "")) || !h.GetAttributeValue("style", "").ToLower().Contains("display: none"))
				?.Count() ?? 0;
			int h1Count = htmlDoc.DocumentNode.SelectNodes("//h1")
				?.Where(h => string.IsNullOrEmpty(h.GetAttributeValue("style", "")) || !h.GetAttributeValue("style", "").ToLower().Contains("display: none"))
				?.Count() ?? 0;

			// Đếm ảnh không có alt
			int imageCountWithoutAlt = htmlDoc.DocumentNode.SelectNodes("//img[not(@alt) or normalize-space(@alt)='']")
				?.Where(img => img.Ancestors("script").Count() == 0 && img.Ancestors("style").Count() == 0)
				?.Count() ?? 0;

			// Tính KeywordDensity
			var normalizedKeyword = RemoveDiacritics(mainKeyword);
			var keywordWords = normalizedKeyword.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var keywordPhraseCount = 0;
			var wordArray = words.ToArray();
			for (int i = 0; i < wordArray.Length - keywordWords.Length + 1; i++)
			{
				var phrase = string.Join(" ", wordArray.Skip(i).Take(keywordWords.Length));
				if (phrase.Equals(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
				{
					keywordPhraseCount++;
				}
			}
			var keywordWordCount = keywordWords.Sum(kw => words.Count(w => w.Equals(kw, StringComparison.OrdinalIgnoreCase)));
			double keywordDensity = keywordPhraseCount > 0 ? (keywordPhraseCount * 100.0 / words.Count()) : (keywordWordCount > 0 ? (keywordWordCount / keywordWords.Length * 100.0 / words.Count()) : 0);
			keywordDensity = Math.Max(keywordDensity, Math.Min(keywordDensity + (keywordWordCount / keywordWords.Length * 100.0 / words.Count()) * 0.3, 3.0));

			// Đếm InternalLink và BrokenLink
			var domain = new Uri(url).Host;
			int internalLinkCount = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
				?.Count(a => !string.IsNullOrEmpty(a.GetAttributeValue("href", "")) && (a.GetAttributeValue("href", "").StartsWith("/") || a.GetAttributeValue("href", "").Contains(domain))) ?? 0;

			int brokenLinkCount = 0;
			var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
			if (links != null)
			{
				foreach (var link in links.Take(20))
				{
					var href = link.GetAttributeValue("href", "");
					if (!string.IsNullOrEmpty(href) && href.StartsWith("http"))
					{
						try
						{
							var response = await httpClient.GetAsync(href);
							if (!response.IsSuccessStatusCode) brokenLinkCount++;
						}
						catch
						{
							brokenLinkCount++;
						}
					}
				}
			}

			// Kiểm tra Canonical URL và Structured Data
			bool hasCanonicalUrl = htmlDoc.DocumentNode.SelectSingleNode("//link[@rel='canonical']") != null;
			bool hasStructuredData = htmlDoc.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']") != null;

			return (title, metaDescription, wordCount, headingCount, h1Count, imageCountWithoutAlt, keywordDensity, internalLinkCount, brokenLinkCount, hasCanonicalUrl, hasStructuredData);
		}

		private async Task<int> AnalyzePageSpeed(string url, string strategy)
		{
			var httpClient = _httpClientFactory.CreateClient();
			var apiUrl = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&key={_pageSpeedApiKey}&strategy={strategy}";
			try
			{
				var response = await httpClient.GetFromJsonAsync<PageSpeedResponse>(apiUrl);
				if (response?.LighthouseResult?.Categories?.Performance?.Score != null)
				{
					return (int)(response.LighthouseResult.Categories.Performance.Score * 100);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to fetch Page Speed score for URL: {Url}, Strategy: {Strategy}", url, strategy);
			}
			return 0; // Fallback nếu API thất bại
		}

		private string GenerateSummary(SEOOnPageAnalysisResult result)
		{
			var issues = new List<string>();
			if (!result.IsTitleLengthOptimal) issues.Add("Độ dài tiêu đề không tối ưu (nên từ 30-60 ký tự). Gợi ý: Rút ngắn hoặc mở rộng tiêu đề để nằm trong khoảng này.");
			if (!result.IsMetaDescriptionLengthOptimal) issues.Add("Độ dài Meta Description không tối ưu (nên từ 120-160 ký tự). Gợi ý: Điều chỉnh Meta Description để nằm trong khoảng này.");
			if (!result.IsMainKeywordInTitle) issues.Add($"Tiêu đề thiếu từ khóa chính.");
			if (!result.IsMainKeywordInMetaDescription) issues.Add($"Meta Description thiếu từ khóa chính.");
			if (!result.IsWordCountSufficient) issues.Add($"Số lượng từ quá ít, tối thiểu 300 từ). Gợi ý: Bổ sung nội dung để đạt ít nhất 300 từ.");
			if (result.H1Count != 1) issues.Add($"Trang nên có đúng 1 thẻ H1 (hiện có {result.H1Count}). Gợi ý: Đảm bảo chỉ có 1 thẻ H1 duy nhất.");
			if (result.ImageCountWithoutAlt > 0) issues.Add($"{result.ImageCountWithoutAlt} hình ảnh thiếu alt text. Gợi ý: Thêm thuộc tính alt mô tả nội dung hình ảnh.");
			if (result.KeywordDensity < 1) issues.Add($"Mật độ từ khóa ({result.KeywordDensity:F1}%) quá thấp (nên là 1-3%). Gợi ý: Thêm từ khóa vào nội dung, ví dụ trong các đoạn văn hoặc tiêu đề phụ.");
			if (result.KeywordDensity > 3) issues.Add($"Mật độ từ khóa ({result.KeywordDensity:F1}%) quá cao (nên là 1-3%).");
			if (result.HeadingCount == 0) issues.Add("Không tìm thấy tiêu đề (H1, H2, H3) trên trang. Gợi ý: Thêm ít nhất 1 thẻ H1 và các thẻ H2/H3 để cấu trúc nội dung.");
			if (result.BrokenLinkCount > 0) issues.Add($"Có {result.BrokenLinkCount} liên kết hỏng trên trang. Gợi ý: Kiểm tra và sửa các liên kết bị lỗi (404, 500).");
			if (!result.HasCanonicalUrl) issues.Add("Thiếu thẻ canonical URL. Gợi ý: Thêm thẻ <link rel='canonical'> để tránh trùng lặp nội dung.");
			if (!result.HasStructuredData) issues.Add("Trang chưa sử dụng structured data (schema markup). Gợi ý: Thêm schema markup (ví dụ: Article, FAQ) để tăng khả năng hiển thị trên SERP.");
			if (!result.IsHttps) issues.Add("Trang không sử dụng HTTPS. Gợi ý: Chuyển sang HTTPS để tăng độ tin cậy và bảo mật.");
			if (result.PageSpeedScoreDesktop < 50) issues.Add($"Điểm tốc độ trang desktop ({result.PageSpeedScoreDesktop}) thấp. Gợi ý: Nén hình ảnh, giảm yêu cầu HTTP, sử dụng lazy loading.");
			if (result.PageSpeedScoreMobile < 50) issues.Add($"Điểm tốc độ trang mobile ({result.PageSpeedScoreMobile}) thấp. Gợi ý: Tối ưu hóa hình ảnh, sử dụng AMP nếu cần.");

			return issues.Count == 0
				? "Trang web đạt các tiêu chí SEO On-Page cơ bản."
				: $"Trang web cần cải thiện: {string.Join(", ", issues)}";
		}

		public class PageSpeedResponse
		{
			public LighthouseResult LighthouseResult { get; set; }
		}

		public class LighthouseResult
		{
			public Categories Categories { get; set; }
		}

		public class Categories
		{
			public Performance Performance { get; set; }
		}

		public class Performance
		{
			public double Score { get; set; }
		}
	}
}
