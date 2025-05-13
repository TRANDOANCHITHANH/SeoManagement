using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class KeywordsController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<KeywordsController> _logger;

		public KeywordsController(HttpClient httpClient, IConfiguration configuration, ILogger<KeywordsController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_logger = logger;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
		}

		public async Task<IActionResult> Index(int? projectId, int pageNumber = 1, int pageSize = 10)
		{
			if (!projectId.HasValue)
			{
				TempData["Error"] = "Vui lòng chọn một dự án để xem danh sách từ khóa.";
				return RedirectToAction("Index", "SEOProjects");
			}

			try
			{
				var projectResponse = await _httpClient.GetAsync($"api/seoprojects/{projectId}");
				if (!projectResponse.IsSuccessStatusCode)
				{
					TempData["Error"] = "Dự án không tồn tại. Vui lòng chọn dự án khác.";
					return RedirectToAction("Index", "SEOProjects");
				}

				var project = await projectResponse.Content.ReadFromJsonAsync<SEOProjectViewModel>();
				if (project == null)
				{
					TempData["Error"] = "Không thể tải dữ liệu dự án.";
					return RedirectToAction("Index", "SEOProjects");
				}

				var response = await _httpClient.GetAsync($"api/keywords/project/{projectId}?pageNumber={pageNumber}&pageSize={pageSize}");
				if (!response.IsSuccessStatusCode)
				{
					TempData["Error"] = "Không thể tải danh sách từ khóa. Vui lòng thử lại sau.";
					return View(new PagedResultViewModel<KeywordViewModel> { Items = new List<KeywordViewModel>() });
				}

				var result = await response.Content.ReadFromJsonAsync<PagedResultViewModel<KeywordViewModel>>();
				if (result == null)
				{
					TempData["Error"] = "Dữ liệu trả về không hợp lệ.";
					return View(new PagedResultViewModel<KeywordViewModel> { Items = new List<KeywordViewModel>() });
				}

				ViewBag.ProjectId = projectId;
				ViewBag.ProjectName = project.ProjectName;
				return View(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi gọi API để lấy danh sách từ khóa cho projectId: {ProjectId}", projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách từ khóa.";
				return View(new PagedResultViewModel<KeywordViewModel> { Items = new List<KeywordViewModel>() });
			}
		}


		public async Task<IActionResult> Create(int projectId)
		{
			var projectResponse = await _httpClient.GetAsync($"api/seoprojects/{projectId}");
			if (!projectResponse.IsSuccessStatusCode)
			{
				TempData["Error"] = "Dự án không tồn tại. Vui lòng chọn dự án khác.";
				return RedirectToAction("Index", "SEOProjects");
			}

			var project = await projectResponse.Content.ReadFromJsonAsync<SEOProjectViewModel>();
			if (project == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu dự án.";
				return RedirectToAction("Index", "SEOProjects");
			}

			var model = new KeywordViewModel { ProjectID = projectId };
			ViewBag.ProjectName = project.ProjectName;
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("KeywordID,ProjectID,KeywordName,SearchVolume,Competition,CurrentRank,SearchIntent,CreatedDate")]
		KeywordViewModel keyword)
		{
			if (!ModelState.IsValid)
			{
				return View(keyword);
			}

			try
			{
				var projectResponse = await _httpClient.GetAsync($"api/seoprojects/{keyword.ProjectID}");
				if (!projectResponse.IsSuccessStatusCode)
				{
					_logger.LogWarning("ProjectID {ProjectID} không tồn tại", keyword.ProjectID);
					ModelState.AddModelError("", "Dự án không tồn tại. Vui lòng chọn một dự án hợp lệ.");
					return View(keyword);
				}

				var project = await projectResponse.Content.ReadFromJsonAsync<SEOProjectViewModel>();
				if (project == null)
				{
					_logger.LogWarning("Không thể tải dữ liệu dự án với ProjectID: {ProjectID}", keyword.ProjectID);
					ModelState.AddModelError("", "Không thể tải dữ liệu dự án.");
					return View(keyword);
				}

				var response = await _httpClient.PostAsJsonAsync("api/keywords/create", keyword);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Từ khóa đã được tạo thành công.";
					return RedirectToAction(nameof(Index), new { projectId = keyword.ProjectID });
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo từ khóa: {ErrorContent}", errorContent);
				ModelState.AddModelError("", "Không thể tạo từ khóa. Vui lòng thử lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi tạo từ khóa");
				ModelState.AddModelError("", "Không thể tạo từ khóa do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(keyword);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var response = await _httpClient.GetAsync($"api/keywords/{id}");
			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Không tìm thấy từ khóa để chỉnh sửa.";
				return RedirectToAction("Index", new { projectId = ViewBag.ProjectId });
			}

			var keyword = await response.Content.ReadFromJsonAsync<KeywordViewModel>();
			if (keyword == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu từ khóa.";
				return RedirectToAction("Index", new { projectId = ViewBag.ProjectId });
			}

			var projectResponse = await _httpClient.GetAsync($"api/seoprojects/{keyword.ProjectID}");
			if (projectResponse.IsSuccessStatusCode)
			{
				var project = await projectResponse.Content.ReadFromJsonAsync<SEOProjectViewModel>();
				ViewBag.ProjectName = project?.ProjectName;
			}

			return View(keyword);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, KeywordViewModel keyword)
		{
			if (id != keyword.KeywordID)
			{
				ModelState.AddModelError("", "ID không khớp.");
				return View(keyword);
			}

			if (!ModelState.IsValid)
			{
				return View(keyword);
			}

			try
			{
				var response = await _httpClient.PutAsJsonAsync($"api/keywords/{id}", keyword);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Cập nhật từ khóa thành công.";
					return RedirectToAction(nameof(Index), new { projectId = keyword.ProjectID });
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi cập nhật từ khóa: {ErrorContent}", errorContent);
				ModelState.AddModelError("", "Không thể cập nhật từ khóa. Vui lòng thử lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi cập nhật từ khóa");
				ModelState.AddModelError("", "Không thể cập nhật từ khóa do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(keyword);
		}

		public async Task<IActionResult> Delete(int id, int projectId)
		{
			var response = await _httpClient.DeleteAsync($"api/keywords/{id}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Từ khóa đã được xóa thành công.";
				return RedirectToAction(nameof(Index), new { projectId });
			}

			TempData["Error"] = "Không thể xóa từ khóa. Vui lòng thử lại.";
			return RedirectToAction(nameof(Index), new { projectId });
		}

		public async Task<IActionResult> Details(int id)
		{
			var response = await _httpClient.GetAsync($"api/keywords/{id}");
			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Không tìm thấy từ khóa để xem chi tiết.";
				return NotFound();
			}

			var keyword = await response.Content.ReadFromJsonAsync<KeywordViewModel>();
			if (keyword == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu từ khóa.";
				return NotFound();
			}

			var projectResponse = await _httpClient.GetAsync($"api/seoprojects/{keyword.ProjectID}");
			if (projectResponse.IsSuccessStatusCode)
			{
				var project = await projectResponse.Content.ReadFromJsonAsync<SEOProjectViewModel>();
				ViewBag.ProjectName = project?.ProjectName;
			}

			return View(keyword);
		}

	}

}

