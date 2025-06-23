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

		[HttpGet]
		public async Task<IActionResult> Index(int? projectId = null, string query = null)
		{
			var model = new IntentViewModel
			{
				ProjectId = projectId ?? 0,
				Query = query ?? "",
				MainIntents = new List<IntentScore>(),
				SubIntents = new List<IntentScore>()
			};

			if (!string.IsNullOrEmpty(query) && projectId.HasValue)
			{
				try
				{
					var payload = new { keyword = query, content = "" }; // Có thể lấy content từ KeywordResearch nếu cần
					var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
					var response = await _httpClient.PostAsync("predict", content);

					response.EnsureSuccessStatusCode();
					var result = await response.Content.ReadAsStringAsync();
					_logger.LogInformation("API Response: {Result}", result);
					var data = JsonSerializer.Deserialize<ApiResponse>(result);
					model.MainIntents = data.MainIntents ?? new List<IntentScore>();
					model.SubIntents = data.SubIntents ?? new List<IntentScore>();
					model.SeoSuggestion = GetDetailedSeoSuggestion(model.MainIntents.FirstOrDefault()?.Intent ?? "None", query, "", model.SubIntents);
					model.AiContentSuggestion = null;
				}
				catch (HttpRequestException ex)
				{
					_logger.LogError(ex, "HTTP Request failed");
					ModelState.AddModelError(string.Empty, $"Request failed: {ex.Message}");
				}
			}

			return View(model);
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
				var payload = new { keyword = model.Query, content = model.Content ?? "" };
				var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync("predict", content);

				response.EnsureSuccessStatusCode();
				var result = await response.Content.ReadAsStringAsync();
				_logger.LogInformation("API Response: {Result}", result);
				var data = JsonSerializer.Deserialize<ApiResponse>(result);
				model.MainIntents = data.MainIntents ?? new List<IntentScore>();
				model.SubIntents = data.SubIntents ?? new List<IntentScore>();
				model.SeoSuggestion = GetDetailedSeoSuggestion(model.MainIntents.FirstOrDefault()?.Intent ?? "None", model.Query, model.Content, model.SubIntents);
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

		private string GetDetailedSeoSuggestion(string intent, string query, string existingContent, List<IntentScore> subIntents)
		{
			var lsiKeywords = GetLsiKeywords(query, subIntents, existingContent);
			var isProductQuery = query.Contains("máy giặt") || query.Contains("electrolux") || existingContent?.Contains("máy giặt") == true;
			var wordCount = query.Split(' ').Length;
			var isLongTail = wordCount > 2;

			var suggestionBuilder = new StringBuilder();

			// Technical SEO (cơ bản cho mọi intent)
			suggestionBuilder.AppendLine("Tối ưu kỹ thuật: Đảm bảo tốc độ tải < 3s, responsive trên mobile, sử dụng schema markup (nếu là sản phẩm).");

			// Gợi ý dựa trên intent
			switch (intent)
			{
				case "Informational":
					suggestionBuilder.AppendLine($"Tạo bài viết {query} dài {(isLongTail ? "1000-1500" : "1500-2000")} từ.");
					suggestionBuilder.AppendLine($"Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Giới thiệu', 'Thông tin chi tiết', 'Kết luận'.");
					suggestionBuilder.AppendLine("Thêm FAQ, hình ảnh minh họa, meta description 150-160 ký tự, internal link đến bài liên quan.");
					break;

				case "Commercial Investigation":
					suggestionBuilder.AppendLine($"So sánh {query} với sản phẩm cạnh tranh (VD: Samsung, LG).");
					suggestionBuilder.AppendLine($"Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Đánh giá', 'So sánh', 'Ưu điểm nổi bật'.");
					suggestionBuilder.AppendLine("Thêm bảng so sánh, video review, backlink từ trang công nghệ uy tín.");
					if (isProductQuery) suggestionBuilder.AppendLine("Tối ưu nút CTA 'Xem chi tiết' hoặc 'Mua ngay'.");
					break;

				case "Transactional":
					suggestionBuilder.AppendLine($"Tối ưu trang {query} với nút CTA 'Mua ngay' hoặc 'Đặt hàng'.");
					suggestionBuilder.AppendLine($"Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Ưu đãi', 'Hướng dẫn mua', 'Đánh giá khách hàng'.");
					suggestionBuilder.AppendLine("Đảm bảo tốc độ tải < 2s, thêm đánh giá sao, hình ảnh sản phẩm chất lượng cao.");
					break;

				case "Navigational":
					suggestionBuilder.AppendLine($"Tối ưu trang đích {query} với breadcrumb, sitemap XML.");
					suggestionBuilder.AppendLine($"Sử dụng từ khóa phụ: {string.Join(", ", lsiKeywords)}. Cấu trúc: H1: {query}, H2: 'Hướng dẫn truy cập', 'Liên kết liên quan'.");
					suggestionBuilder.AppendLine("Tối ưu tốc độ < 2s, thêm link đến trang chính.");
					break;

				default:
					suggestionBuilder.AppendLine($"Tối ưu nội dung {query} với từ khóa phụ: {string.Join(", ", lsiKeywords)}.");
					suggestionBuilder.AppendLine("Sử dụng cấu trúc H1: {query}, H2: 'Tổng quan', 'Chi tiết'.");
					break;
			}

			// Off-page SEO (chung cho mọi intent)
			suggestionBuilder.AppendLine("Xây dựng off-page: Đăng bài trên mạng xã hội, lấy backlink từ blog công nghệ, khuyến khích chia sẻ khách hàng.");

			return suggestionBuilder.ToString().Trim();
		}

		private string[] GetLsiKeywords(string query, List<IntentScore> subIntents, string existingContent)
		{
			var keywords = new List<string>();
			var isProduct = query.Contains("máy giặt") || query.Contains("electrolux") || existingContent?.Contains("máy giặt") == true;

			// Dựa trên query
			if (query.Contains("mua")) keywords.AddRange(new[] { "giá rẻ", "khuyến mãi", "ưu đãi" });
			if (query.Contains("so sánh")) keywords.AddRange(new[] { "đánh giá", "so sánh giá", "ưu điểm" });
			if (query.Contains("hướng dẫn")) keywords.AddRange(new[] { "cách sử dụng", "bảo quản" });

			// Dựa trên ngành (máy giặt)
			if (isProduct)
			{
				keywords.AddRange(new[] { "máy giặt cửa trước", "máy giặt inverter", "đánh giá electrolux", "bảo hành" });
			}

			// Dựa trên sub-intents
			if (subIntents.Any(s => s.Intent == "Statistics/Facts")) keywords.AddRange(new[] { "thống kê", "dữ liệu sử dụng" });
			if (subIntents.Any(s => s.Intent == "Solutions/How-to")) keywords.AddRange(new[] { "hướng dẫn lắp đặt", "bảo trì" });
			if (subIntents.Any(s => s.Intent == "Reviews")) keywords.AddRange(new[] { "đánh giá người dùng", "phản hồi" });

			// Loại bỏ trùng lặp và giới hạn 5 từ khóa
			return keywords.Distinct().Take(5).ToArray();
		}
	}
}

