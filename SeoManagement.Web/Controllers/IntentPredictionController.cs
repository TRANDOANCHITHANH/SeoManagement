using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities.Dtos;
using SeoManagement.Core.Interfaces;
using SeoManagement.Web.Models.ViewModels;
using System.Text;
using System.Text.Json;

namespace SeoManagement.Web.Controllers
{
	public class IntentPredictionController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IAiContentService _aiContentService;
		private readonly ILogger<IntentPredictionController> _logger;
		public IntentPredictionController(IHttpClientFactory httpClientFactory, IAiContentService aiContentService, ILogger<IntentPredictionController> logger)
		{
			_httpClient = httpClientFactory.CreateClient();
			_httpClient.BaseAddress = new Uri("http://localhost:8000/");
			_aiContentService = aiContentService;
			_logger = logger;
		}

		public IActionResult Index()
		{
			return View(new IntentViewModel
			{
				MainIntents = new List<IntentScore>(),
				SubIntents = new List<IntentScore>()
			});
		}

		[HttpPost]
		public async Task<IActionResult> Index(IntentViewModel model)
		{
			if (!ModelState.IsValid)
			{
				model.MainIntents = new List<IntentScore>();
				model.SubIntents = new List<IntentScore>();
				return View(model);
			}

			try
			{
				var payload = new { keyword = model.Query, content = model.Content };
				var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync("predict", content);

				response.EnsureSuccessStatusCode();
				var result = await response.Content.ReadAsStringAsync();
				_logger.LogInformation("API Response: {Result}", result);
				var data = JsonSerializer.Deserialize<ApiResponse>(result);
				model.MainIntents = data.MainIntents ?? new List<IntentScore>();
				model.SubIntents = data.SubIntents ?? new List<IntentScore>();
				model.SeoSuggestion = GetDetailedSeoSuggestion(model.MainIntents.FirstOrDefault()?.Intent ?? "None", model.Query, model.SubIntents);
				// Không tạo nội dung AI ngay, để trống
				model.AiContentSuggestion = null;

				return View(model);
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP Request failed");
				ModelState.AddModelError(string.Empty, $"Request failed: {ex.Message}");
				model.MainIntents = new List<IntentScore>();
				model.SubIntents = new List<IntentScore>();
				return View(model);
			}
		}

		[HttpPost]
		public async Task<IActionResult> GenerateAiContent([FromBody] IntentViewModel model)
		{
			_logger.LogInformation("Received GenerateAiContent request: Query={Query}, Content={Content}, MainIntents={MainIntents}, SubIntents={SubIntents}",
			model.Query, model.Content, JsonSerializer.Serialize(model.MainIntents), JsonSerializer.Serialize(model.SubIntents));

			if (string.IsNullOrEmpty(model.Query))
			{
				return Json(new { success = false, message = "Vui lòng phân tích trước." });
			}

			try
			{
				var intent = model.MainIntents?.FirstOrDefault()?.Intent ?? "None";
				var aiContent = await _aiContentService.GenerateAiContentSuggestion(intent, model.Query, model.Content, model.SubIntents ?? new List<IntentScore>());
				_logger.LogInformation("Generated AI content: {AiContent}", aiContent);
				return Json(new { success = true, aiContent });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating AI content");
				return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
			}
		}

		private string GetDetailedSeoSuggestion(string intent, string query, List<IntentScore> subIntents)
		{
			var lsiKeywords = GetLsiKeywords(query, subIntents);
			return intent switch
			{
				"Informational" => $"Tạo bài viết {query} dài 1500-2000 từ. Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Giới thiệu', 'Chi tiết', 'Kết luận'. Thêm FAQ, hình ảnh, meta description 150-160 ký tự. Tối ưu tốc độ tải < 3s.",
				"Commercial Investigation" => $"So sánh {query} với sản phẩm tương tự. Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Đánh giá', 'So sánh', 'Lợi ích'. Thêm video, bảng so sánh, và backlink chất lượng.",
				"Transactional" => $"Tối ưu trang {query} với nút CTA 'Mua ngay'. Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Ưu đãi', 'Hướng dẫn mua'. Đảm bảo tốc độ tải < 3s, thêm đánh giá khách hàng.",
				"Navigational" => $"Tối ưu trang đích {query} với breadcrumb và sitemap. Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Hướng dẫn', 'Liên kết'. Tối ưu tốc độ < 2s.",
				_ => $"Tối ưu nội dung {query} với từ khóa phụ: {string.Join(", ", lsiKeywords)}."
			};
		}

		private string[] GetLsiKeywords(string query, List<IntentScore> subIntents)
		{
			var keywords = new List<string>();
			if (query.Contains("mua")) keywords.AddRange(new[] { "khuyến mãi", "giá rẻ" });
			if (query.Contains("sản xuất")) keywords.AddRange(new[] { "công nghệ", "quy trình" });
			if (query.Contains("biến đổi")) keywords.AddRange(new[] { "ảnh hưởng", "môi trường" });
			if (subIntents.Any(s => s.Intent == "Statistics/Facts")) keywords.Add("dữ liệu");
			if (subIntents.Any(s => s.Intent == "Solutions/How-to")) keywords.Add("hướng dẫn");
			return keywords.ToArray();
		}
	}
}

