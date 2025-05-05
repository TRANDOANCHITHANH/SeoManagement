using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Web.Models.ViewModels;
using System.Text.Json;

namespace SeoManagement.Web.Controllers
{
	public class SEOOnPageChecksController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IConfiguration _configuration;
		private readonly ILogger<SEOOnPageChecksController> _logger;


		public SEOOnPageChecksController(HttpClient httpClient, UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger<SEOOnPageChecksController> logger)
		{
			_httpClient = httpClient;
			_userManager = userManager;
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

		[HttpGet]
		public IActionResult CreateSEOOnPageProject()
		{
			return View(new SEOProjectViewModel
			{
				ProjectType = "SEOOnPage",
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddDays(30),
				Status = Models.ViewModels.ProjectStatus.Active
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateSEOOnPageProject(SEOProjectViewModel project)
		{
			if (!ModelState.IsValid)
			{
				return View(project);
			}

			if (project.EndDate < project.StartDate)
			{
				ModelState.AddModelError("EndDate", "Ngày kết thúc không thể trước ngày bắt đầu.");
				return View(project);
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
				return RedirectToAction("Login", "Account");
			}

			if (user.Id <= 0)
			{
				_logger.LogError("User ID không hợp lệ: {UserId}", user.Id);
				TempData["Error"] = "User ID không hợp lệ. Vui lòng kiểm tra tài khoản của bạn.";
				return View(project);
			}

			try
			{
				var projectDto = new
				{
					UserID = user.Id,
					ProjectName = project.ProjectName,
					Description = project.Description,
					ProjectType = project.ProjectType,
					StartDate = project.StartDate,
					EndDate = project.EndDate,
					Status = (int)project.Status,
				};

				var response = await _httpClient.PostAsJsonAsync("/api/seoprojects", projectDto);
				if (response.IsSuccessStatusCode)
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					var createdProject = JsonSerializer.Deserialize<SEOProjectViewModel>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					if (createdProject != null && createdProject.ProjectID > 0)
					{
						int projectId = createdProject.ProjectID;
						TempData["Success"] = "Dự án SEO On-Page đã được tạo thành công!";
						return RedirectToAction("Index", "SEOOnPageChecks", new { projectId });
					}
					else
					{
						_logger.LogError("Không thể lấy ProjectID từ response: {ResponseContent}", responseContent);
						TempData["Error"] = "Không thể lấy ID của dự án mới tạo.";
						return View(project);
					}
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError("Lỗi khi tạo dự án SEO On-Page: {ErrorContent}", errorContent);
					TempData["Error"] = "Không thể tạo dự án. Vui lòng thử lại.";
					return View(project);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tạo SEO On-Page Project.");
				TempData["Error"] = "Đã xảy ra lỗi khi tạo dự án: " + ex.Message;
				return View(project);
			}
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
