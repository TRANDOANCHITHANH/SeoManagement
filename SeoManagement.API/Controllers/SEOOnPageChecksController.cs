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
				_logger.LogInformation("Starting SEO On-Page analysis for CheckID {ID}. URL: {Url}, MainKeyword: {Keyword}", id, check.Url, check.MainKeyword);
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
					HasStructuredData = analysisResult.HasStructuredData,
					HasViewport = analysisResult.HasViewport,
					HasMetaRobots = analysisResult.HasMetaRobots,
					HasOpenGraph = analysisResult.HasOpenGraph,
					HasTwitterCards = analysisResult.HasTwitterCards,
					UrlStructureFeedback = analysisResult.UrlStructureFeedback,
					DuplicateContentCount = analysisResult.DuplicateContentCount
				};

				// Kiểm tra HTTPS
				result.IsHttps = !string.IsNullOrEmpty(check.Url) && Uri.TryCreate(check.Url, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;

				// Phân tích tốc độ trang
				var pageSpeedTasks = new[] { AnalyzePageSpeedWithRetry(check.Url, "desktop"), AnalyzePageSpeedWithRetry(check.Url, "mobile") };
				var speedResults = await Task.WhenAll(pageSpeedTasks);
				result.PageSpeedScoreDesktop = speedResults[0];
				result.PageSpeedScoreMobile = speedResults[1];

				result.Summary = GenerateSummary(result);
				_logger.LogInformation("SEO On-Page analysis completed for CheckID {ID}. Result: {Summary}", id, result.Summary);
				return Ok(result);
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP error analyzing SEO On-Page for CheckID: {CheckID}. URL: {Url}", id, check.Url);
				return StatusCode(500, "Lỗi kết nối khi phân tích SEO On-Page. Vui lòng thử lại sau.");
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

		private async Task<(string Title, string MetaDescription, int WordCount, int HeadingCount, int H1Count, int ImageCountWithoutAlt, double KeywordDensity, int InternalLinkCount, int BrokenLinkCount, bool HasCanonicalUrl, bool HasStructuredData, bool HasViewport, bool HasMetaRobots, bool HasOpenGraph, bool HasTwitterCards, string UrlStructureFeedback, int DuplicateContentCount)> AnalyzeHtml(string url, string mainKeyword, ILogger logger)
		{
			var httpClient = _httpClientFactory.CreateClient();
			httpClient.Timeout = TimeSpan.FromSeconds(30);
			string html;
			try
			{
				_logger.LogInformation("Fetching HTML for URL: {Url}", url);
				using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();
				html = await response.Content.ReadAsStringAsync();
				html = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Convert(System.Text.Encoding.Default, System.Text.Encoding.UTF8, System.Text.Encoding.Default.GetBytes(html)));
			}
			catch (HttpRequestException ex)
			{
				logger.LogWarning(ex, "Failed to fetch HTML for URL: {Url}. Status: {StatusCode}", url, ex.StatusCode);
				return (null, null, 0, 0, 0, 0, 0, 0, 0, false, false, false, false, false, false, "Không thể phân tích URL do lỗi kết nối.", 0);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unexpected error fetching HTML for URL: {Url}", url);
				return (null, null, 0, 0, 0, 0, 0, 0, 0, false, false, false, false, false, false, "Không thể phân tích URL do lỗi không xác định.", 0);
			}

			var htmlDoc = new HtmlDocument();
			try
			{
				htmlDoc.LoadHtml(html);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to parse HTML for URL: {Url}", url);
				return (null, null, 0, 0, 0, 0, 0, 0, 0, false, false, false, false, false, false, "Không thể phân tích URL do lỗi cú pháp HTML.", 0);
			}

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
			int headingCount = htmlDoc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6")
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
				foreach (var link in links.Take(50))
				{
					var href = link.GetAttributeValue("href", "");
					if (!string.IsNullOrEmpty(href) && href.StartsWith("http"))
					{
						try
						{
							var response = await httpClient.GetAsync(href, HttpCompletionOption.ResponseHeadersRead);
							if (!response.IsSuccessStatusCode) brokenLinkCount++;
						}
						catch
						{
							brokenLinkCount++;
						}
					}
				}
			}

			// Kiểm tra Canonical URL, Structured Data, Viewport, Meta Robots, Open Graph, Twitter Cards
			bool hasCanonicalUrl = htmlDoc.DocumentNode.SelectSingleNode("//link[@rel='canonical']") != null;
			bool hasStructuredData = htmlDoc.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']") != null;
			bool hasViewport = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='viewport']") != null;
			bool hasMetaRobots = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='robots']") != null;
			bool hasOpenGraph = htmlDoc.DocumentNode.SelectSingleNode("//meta[starts-with(@property, 'og:')]") != null; // Sửa cú pháp
			bool hasTwitterCards = htmlDoc.DocumentNode.SelectSingleNode("//meta[starts-with(@name, 'twitter:')]") != null; // Sửa cú pháp

			// Kiểm tra Duplicate Content
			var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p")?.Select(p => p.InnerText.Trim())?.Where(p => !string.IsNullOrEmpty(p)) ?? new List<string>();
			var duplicateContentCount = paragraphs.GroupBy(p => p).Where(g => g.Count() > 1).Sum(g => g.Count() - 1);

			// Phân tích URL structure
			var uri = new Uri(url);
			string urlStructureFeedback = uri.PathAndQuery.Length > 60 ? "URL quá dài (>60 ký tự). Gợi ý: Rút ngắn URL." :
				(!uri.Segments.Any(s => s.Contains(RemoveDiacritics(mainKeyword).ToLower()))) ? "URL không chứa từ khóa chính. Gợi ý: Thêm từ khóa vào URL." : "URL tối ưu.";

			return (title, metaDescription, wordCount, headingCount, h1Count, imageCountWithoutAlt, keywordDensity, internalLinkCount, brokenLinkCount, hasCanonicalUrl, hasStructuredData, hasViewport, hasMetaRobots, hasOpenGraph, hasTwitterCards, urlStructureFeedback, duplicateContentCount);
		}

		private async Task<int> AnalyzePageSpeedWithRetry(string url, string strategy, int maxRetries = 2, int delayMs = 1000)
		{
			var httpClient = _httpClientFactory.CreateClient();
			var apiUrl = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&key={_pageSpeedApiKey}&strategy={strategy}";
			for (int retry = 0; retry < maxRetries; retry++)
			{
				try
				{
					var response = await httpClient.GetFromJsonAsync<PageSpeedResponse>(apiUrl);
					return (int?)(response?.LighthouseResult?.Categories?.Performance?.Score * 100) ?? 0;
				}
				catch (HttpRequestException ex) when (retry < maxRetries - 1)
				{
					_logger.LogWarning(ex, "Retry {Retry} failed for Page Speed score for URL: {Url}, Strategy: {Strategy}", retry + 1, url, strategy);
					await Task.Delay(delayMs);
				}
			}
			_logger.LogWarning("Max retries reached for Page Speed score for URL: {Url}, Strategy: {Strategy}", url, strategy);
			return 0;
		}

		private string GenerateSummary(SEOOnPageAnalysisResult result)
		{
			var issues = new List<string>();
			if (!result.IsTitleLengthOptimal) issues.Add("Độ dài tiêu đề không tối ưu (nên từ 30-60 ký tự). Gợi ý: Rút ngắn hoặc mở rộng tiêu đề.");
			if (!result.IsMetaDescriptionLengthOptimal) issues.Add("Độ dài Meta Description không tối ưu (nên từ 120-160 ký tự). Gợi ý: Điều chỉnh Meta Description.");
			if (!result.IsMainKeywordInTitle) issues.Add($"Tiêu đề thiếu từ khóa chính.");
			if (!result.IsMainKeywordInMetaDescription) issues.Add($"Meta Description thiếu từ khóa chính.");
			if (!result.IsWordCountSufficient) issues.Add($"Số lượng từ quá ít ({result.WordCount}, tối thiểu 300 từ). Gợi ý: Bổ sung nội dung.");
			if (result.H1Count != 1) issues.Add($"Trang nên có đúng 1 thẻ H1 (hiện có {result.H1Count}). Gợi ý: Đảm bảo chỉ 1 H1.");
			if (result.ImageCountWithoutAlt > 0) issues.Add($"{result.ImageCountWithoutAlt} hình ảnh thiếu alt text. Gợi ý: Thêm alt text.");
			if (result.KeywordDensity < 1) issues.Add($"Mật độ từ khóa ({result.KeywordDensity:F1}%) quá thấp (nên 1-3%). Gợi ý: Thêm từ khóa.");
			if (result.KeywordDensity > 3) issues.Add($"Mật độ từ khóa ({result.KeywordDensity:F1}%) quá cao (nên 1-3%). Gợi ý: Giảm từ khóa.");
			if (result.HeadingCount == 0) issues.Add("Không tìm thấy heading (H1-H6). Gợi ý: Thêm ít nhất 1 H1.");
			if (result.BrokenLinkCount > 0) issues.Add($"Có {result.BrokenLinkCount} liên kết hỏng. Gợi ý: Sửa lỗi 404/500.");
			if (!result.HasCanonicalUrl) issues.Add("Thiếu thẻ canonical URL. Gợi ý: Thêm <link rel='canonical'>.");
			if (!result.HasStructuredData) issues.Add("Thiếu structured data. Gợi ý: Thêm schema markup.");
			if (!result.HasViewport) issues.Add("Thiếu meta viewport. Gợi ý: Thêm <meta name='viewport' content='width=device-width, initial-scale=1'>.");
			if (!result.HasMetaRobots) issues.Add("Thiếu meta robots. Gợi ý: Thêm <meta name='robots' content='index, follow'>.");
			if (!result.HasOpenGraph) issues.Add("Thiếu Open Graph tags. Gợi ý: Thêm meta OG cho chia sẻ.");
			if (!result.HasTwitterCards) issues.Add("Thiếu Twitter Cards. Gợi ý: Thêm meta Twitter.");
			if (result.DuplicateContentCount > 0) issues.Add($"Có {result.DuplicateContentCount} đoạn nội dung trùng lặp. Gợi ý: Loại bỏ nội dung lặp.");
			if (!string.IsNullOrEmpty(result.UrlStructureFeedback)) issues.Add(result.UrlStructureFeedback);
			if (!result.IsHttps) issues.Add("Không dùng HTTPS. Gợi ý: Chuyển sang HTTPS.");
			if (result.PageSpeedScoreDesktop < 50) issues.Add($"Điểm tốc độ desktop ({result.PageSpeedScoreDesktop}%) thấp. Gợi ý: Tối ưu hóa.");
			if (result.PageSpeedScoreMobile < 50) issues.Add($"Điểm tốc độ mobile ({result.PageSpeedScoreMobile}%) thấp. Gợi ý: Tối ưu hóa.");

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
