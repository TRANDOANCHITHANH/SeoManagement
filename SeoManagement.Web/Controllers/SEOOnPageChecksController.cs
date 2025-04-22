using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	public class SEOOnPageChecksController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<SEOOnPageChecksController> _logger;


		public SEOOnPageChecksController(HttpClient httpClient, IConfiguration configuration, ILogger<SEOOnPageChecksController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
		}

		public async Task<IActionResult> Index(int projectId, int pageNumber = 1, int pageSize = 10)
		{
			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOOnPageCheckViewModel>>(
						   $"api/seoonpagechecks/project/{projectId}?pageNumber={pageNumber}&pageSize={pageSize}");

			if (response == null)
			{
				_logger.LogWarning("Không lấy được danh sách kiểm tra SEO On-Page cho ProjectId: {ProjectId}", projectId);
				return View(new PagedResultViewModel<SEOOnPageCheckViewModel> { Items = new List<SEOOnPageCheckViewModel>() });
			}

			ViewBag.ProjectId = projectId;
			return View(response);
		}

		public IActionResult Create(int projectId)
		{
			var model = new SEOOnPageCheckViewModel { ProjectID = projectId };
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SEOOnPageCheckViewModel check)
		{
			if (!ModelState.IsValid)
				return View(check);

			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/seoonpagechecks/create", check);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Kiểm tra SEO On-Page đã được tạo thành công";
					return RedirectToAction(nameof(Index), new { projectId = check.ProjectID });
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo kiểm tra SEO On-Page: {ErrorContent}", errorContent);
				ModelState.AddModelError("", $"Có lỗi xảy ra khi kiểm tra SEO On-Page. Vui lòng thử lại");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi tạo kiểm tra SEO On-Page");
				ModelState.AddModelError("", "Không thể tạo kiểm tra do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(check);
		}

		public async Task<IActionResult> Details(int id)
		{
			var response = await _httpClient.GetAsync($"api/seoonpagechecks/{id}");
			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Không tìm thấy kiểm tra để xem chi tiết.";
				return NotFound();
			}

			var check = await response.Content.ReadFromJsonAsync<SEOOnPageCheckViewModel>();
			if (check == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu kiểm tra.";
				return NotFound();
			}

			// Phân tích SEO On-Page
			var analysisResponse = await _httpClient.PostAsync($"api/seoonpagechecks/{id}/analyze", null);
			if (analysisResponse.IsSuccessStatusCode)
			{
				var analysisResult = await analysisResponse.Content.ReadFromJsonAsync<SEOOnPageAnalysisResultViewModel>();
				ViewBag.AnalysisResult = analysisResult;
			}

			return View(check);
		}

		public async Task<IActionResult> Delete(int id, int projectId)
		{
			var response = await _httpClient.DeleteAsync($"api/seoonpagechecks/{id}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Kiểm tra SEO On-Page đã được xóa thành công.";
				return RedirectToAction(nameof(Index), new { projectId });
			}

			TempData["Error"] = "Không thể xóa kiểm tra. Vui lòng thử lại.";
			return RedirectToAction(nameof(Index), new { projectId });
		}
	}
}
