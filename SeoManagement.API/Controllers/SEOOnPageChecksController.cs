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
					Title = c.Title,
					MetaDescription = c.MetaDescription,
					MainKeyword = c.MainKeyword,
					WordCount = c.WordCount,
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
				Title = check.Title,
				MetaDescription = check.MetaDescription,
				MainKeyword = check.MainKeyword,
				WordCount = check.WordCount,
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
				Title = checkDto.Title,
				MetaDescription = checkDto.MetaDescription,
				MainKeyword = checkDto.MainKeyword,
				WordCount = checkDto.WordCount
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
			check.Title = checkDto.Title;
			check.MetaDescription = checkDto.MetaDescription;
			check.MainKeyword = checkDto.MainKeyword;
			check.WordCount = checkDto.WordCount;

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
				var result = new SEOOnPageAnalysisResult
				{
					IsTitleLengthOptimal = check.Title != null && check.Title.Length >= 30 && check.Title.Length <= 60,
					IsMetaDescriptionLengthOptimal = check.MetaDescription != null && check.MetaDescription.Length >= 120 && check.MetaDescription.Length <= 160,
					IsMainKeywordInTitle = check.MainKeyword != null && check.Title != null && check.Title.ToLower().Contains(check.MainKeyword.ToLower()),
					IsMainKeywordInMetaDescription = check.MainKeyword != null && check.MetaDescription != null && check.MetaDescription.ToLower().Contains(check.MainKeyword.ToLower()),
					IsWordCountSufficient = check.WordCount >= 300
				};

				var htmlAnalysis = await AnalyzeHtml(check.Url, check.MainKeyword);
				result.HeadingCount = htmlAnalysis.HeadingCount;
				result.ImageCountWithoutAlt = htmlAnalysis.ImageCountWithoutAlt;
				result.KeywordDensity = htmlAnalysis.KeywordDensity;
				result.InternalLinkCount = htmlAnalysis.InternalLinkCount;

				result.PageSpeedScore = await AnalyzePageSpeed(check.Url);

				result.Summary = GenerateSummary(result);

				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error analyzing SEO On-Page for CheckID: {CheckID}", id);
				return StatusCode(500, "Error analyzing SEO On-Page. Please try again later.");
			}
		}

		private async Task<(int HeadingCount, int ImageCountWithoutAlt, double KeywordDensity, int InternalLinkCount)> AnalyzeHtml(string url, string mainKeyword)
		{
			var httpClient = _httpClientFactory.CreateClient();
			var html = await httpClient.GetStringAsync(url);

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);

			int headingCount = htmlDoc.DocumentNode.SelectNodes("//h1|//h2|//h3")?.Count ?? 0;

			int imageCountWithoutAlt = htmlDoc.DocumentNode.SelectNodes("//img[not(@alt) or @alt='']")?.Count ?? 0;

			var bodyText = htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerText ?? "";
			var words = bodyText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var keywordCount = words.Count(w => w.Equals(mainKeyword, StringComparison.OrdinalIgnoreCase));
			double keywordDensity = words.Length > 0 ? (keywordCount * 100.0 / words.Length) : 0;

			var domain = new Uri(url).Host;
			int internalLinkCount = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
				?.Count(a => a.GetAttributeValue("href", "").Contains(domain) && !a.GetAttributeValue("href", "").StartsWith("http")) ?? 0;

			return (headingCount, imageCountWithoutAlt, keywordDensity, internalLinkCount);
		}

		private async Task<int> AnalyzePageSpeed(string url)
		{
			var httpClient = _httpClientFactory.CreateClient();
			var apiUrl = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(url)}&key={_pageSpeedApiKey}&strategy=desktop";
			var response = await httpClient.GetFromJsonAsync<PageSpeedResponse>(apiUrl);

			if (response?.LighthouseResult?.Categories?.Performance?.Score != null)
			{
				return (int)(response.LighthouseResult.Categories.Performance.Score * 100);
			}
			return 0;
		}

		private string GenerateSummary(SEOOnPageAnalysisResult result)
		{
			var issues = new List<string>();
			if (!result.IsTitleLengthOptimal) issues.Add("Độ dài tiêu đề không tối ưu (nên từ 30-60 ký tự).");
			if (!result.IsMetaDescriptionLengthOptimal) issues.Add("Độ dài của Meta Description không tối ưu (nên từ 120-160 ký tự).");
			if (!result.IsMainKeywordInTitle) issues.Add("Tiêu đề thiếu từ khóa chính.");
			if (!result.IsMainKeywordInMetaDescription) issues.Add("Meta Description thiếu từ khóa chính.");
			if (!result.IsWordCountSufficient) issues.Add("Số lượng từ quá ít (phải có ít nhất 300 từ).");
			if (result.ImageCountWithoutAlt > 0) issues.Add($"{result.ImageCountWithoutAlt} images are missing alt text.");
			if (result.KeywordDensity < 1 || result.KeywordDensity > 3) issues.Add($"Mật độ từ khóa ({result.KeywordDensity:F1}%) không tối ưu (nên là 1-3%).");
			if (result.HeadingCount == 0) issues.Add("Không tìm thấy tiêu đề (H1, H2, H3) trên trang.");
			if (result.PageSpeedScore < 50) issues.Add($"Điểm tốc độ trang ({result.PageSpeedScore}) thấp. Hãy cân nhắc việc tối ưu hóa hiệu suất.");

			return issues.Count == 0
				? "Trang web đạt các tiêu chí SEO On-Page cơ bản."
				: $"Trang web cần cải thiện một số yếu tố SEO On-Page: {string.Join(", ", issues)}";
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
