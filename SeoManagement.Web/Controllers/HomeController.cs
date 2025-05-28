using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models;
using SeoManagement.Web.Models.ViewModels;
using System.Diagnostics;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth", Policy = "UserOnly")]
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;
		private readonly ISEOPerformanceService _performanceService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly AlertService _alertService;
		private readonly ISEOProjectService _seoProjectService;
		public HomeController(ILogger<HomeController> logger, IConfiguration configuration, HttpClient httpClient, ISEOPerformanceService sEOPerformanceService, UserManager<ApplicationUser> userManager, AlertService alertService, ISEOProjectService seoProjectService)
		{
			_logger = logger;
			_configuration = configuration;
			_httpClient = httpClient;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_performanceService = sEOPerformanceService;
			_userManager = userManager;
			_alertService = alertService;
			_seoProjectService = seoProjectService;
		}

		public async Task<IActionResult> Index(int? categoryId)
		{
			var user = await _userManager.GetUserAsync(User);

			if (user != null)
			{
				var projectsResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOProjectViewModel>>(
					$"/api/seoprojects?pageNumber=1&pageSize=1000&userId={user.Id}");
				var projectIds = projectsResponse?.Items.Select(p => p.ProjectID).ToList() ?? new List<int>();
				ViewBag.Projects = projectsResponse?.Items ?? new List<SEOProjectViewModel>();
				ViewBag.ProjectTypes = new[] { "KeywordRankChecker", "IndexChecker", "PageSpeedChecker", "BacklinkChecker" };
				var recentPerformances = new List<SEOPerformanceHistory>();
				foreach (var projectId in projectIds)
				{
					var history = await _performanceService.GetHistoryByProjectIdAsync(projectId);
					var latest = history.OrderByDescending(h => h.RecordedAt).FirstOrDefault();
					if (latest != null)
					{
						recentPerformances.Add(latest);
					}
				}
				var performanceDataForView = recentPerformances
				.OrderByDescending(p => p.RecordedAt)
				.Take(5)
				.Select(p => new
				{
					p.ProjectId,
					p.ProjectType,
					RecordedAt = p.RecordedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					AverageKeywordRank = p.AverageKeywordRank ?? 0,
					AverageOnPageScore = p.AverageOnPageScore ?? 0,
					PageSpeedScore = p.PageSpeedScore ?? 0,
					BacklinkCount = p.BacklinkCount ?? 0,
					IndexedPageCount = p.IndexedPageCount ?? 0,
					UnindexedPageCount = p.UnindexedPageCount ?? 0
				})
				.ToList();
				ViewBag.RecentPerformances = performanceDataForView;
				var alerts = await _alertService.CheckAlertsAsync(user.Id, sendEmail: false);
				ViewBag.Alerts = alerts;
			}
			else
			{
				ViewBag.RecentPerformances = new List<object>();
			}

			var categoryResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<CategoryViewModel>>("api/categories?pageNumber=1&pageSize=100");
			var categories = categoryResponse?.Items?.Where(c => c.IsActive).ToList() ?? new List<CategoryViewModel>();
			ViewBag.Categories = categories;

			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<NewViewModel>>("api/news?pageNumber=1&pageSize=3&isPublished=true");
			if (response == null || !response.Items.Any())
			{
				return View(new PagedResultViewModel<NewViewModel> { Items = new List<NewViewModel>() });
			}

			var configResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SystemConfigViewModel>>("api/systemconfigs?pageNumber=1&pageSize=100");
			var configs = configResponse?.Items?.ToDictionary(c => c.ConfigKey, c => c.ConfigValue) ?? new Dictionary<string, string>();
			ViewBag.Configs = configs;
			ViewBag.SelectedCategoryId = categoryId;
			return View(response);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateMonitoring(List<int> selectedProjects)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				TempData["Error"] = "Không thể xác định thông tin người dùng.";
				return RedirectToAction("Login", "Account");
			}

			var userId = user.Id;
			var projects = (await _seoProjectService.GetPagedAsync(1, int.MaxValue, userId)).Items;

			var projectTypes = new[] { "KeywordRankChecker", "IndexChecker", "PageSpeedChecker", "BacklinkChecker" };
			var selectedByType = new Dictionary<string, int>();

			foreach (var projectId in selectedProjects)
			{
				var project = projects.FirstOrDefault(p => p.ProjectID == projectId);
				if (project != null)
				{
					var projectType = project.ProjectType;
					if (!selectedByType.ContainsKey(projectType) && projectTypes.Contains(projectType))
					{
						selectedByType[projectType] = projectId;
					}
				}
			}

			if (selectedByType.Count != 4)
			{
				return Json(new { success = false, message = "Bạn phải chọn đúng 1 dự án cho mỗi loại: Kiểm tra thứ hạng từ khóa, Kiểm tra Index, Kiểm tra tốc độ tải trang, Kiểm tra Backlink." });
			}

			try
			{
				await _seoProjectService.BeginTransactionAsync();
				foreach (var project in projects.Where(p => p.IsMonitored == true))
				{
					project.IsMonitored = false;
					if (project.AlertConfiguration != null)
					{
						project.AlertConfiguration.IsAlertMonitored = false;
					}
					else
					{
						project.AlertConfiguration = new AlertConfiguration
						{
							ProjectId = project.ProjectID,
							IsAlertMonitored = false
						};
					}
					await _seoProjectService.UpdateSEOProjectAsync(project);
				}
				foreach (var projectId in selectedProjects)
				{
					var project = projects.FirstOrDefault(p => p.ProjectID == projectId);
					if (project != null)
					{
						var projectType = project.ProjectType;
						if (selectedByType.ContainsKey(projectType) && selectedByType[projectType] == projectId)
						{
							project.IsMonitored = true;
							if (project.AlertConfiguration != null)
							{
								project.AlertConfiguration.IsAlertMonitored = true;
							}
							else
							{
								project.AlertConfiguration = new AlertConfiguration
								{
									ProjectId = project.ProjectID,
									IsAlertMonitored = true
								};
							}
							await _seoProjectService.UpdateSEOProjectAsync(project);
						}
					}
				}

				await _seoProjectService.CommitTransactionAsync();
				return Json(new { success = true, message = "Đã cập nhật danh sách dự án theo dõi. Hệ thống sẽ tự động gửi email cảnh báo hàng ngày vào lúc 0h00." });
			}
			catch (Exception ex)
			{
				await _seoProjectService.RollbackTransactionAsync();
				_logger.LogError(ex, "Lỗi khi cập nhật trạng thái theo dõi dự án cho người dùng {UserId}", userId);
				return Json(new { success = false, message = "Đã xảy ra lỗi khi lưu. Vui lòng thử lại." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> SendAlerts()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return Unauthorized();
			}

			var alerts = await _alertService.CheckAlertsAsync(user.Id, sendEmail: true);
			if (alerts.Any())
			{
				TempData["Success"] = "Cảnh báo đã được gửi qua email.";
			}
			else
			{
				TempData["Info"] = "Không có cảnh báo nào để gửi.";
			}
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[Route("/Home/Error/{statusCode?}")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error(int? statusCode = null)
		{
			var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };

			if (statusCode.HasValue)
			{
				model.StatusCode = statusCode.Value;
				if (statusCode == 404)
				{
					return View("NotFound", model);
				}
				if (statusCode >= 500)
				{
					return View("ServerError", model);
				}
			}

			var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
			if (exceptionHandlerPathFeature?.Error != null)
			{
				_logger.LogError(exceptionHandlerPathFeature.Error, "An error occurred at {Path}", exceptionHandlerPathFeature.Path);
				model.ErrorMessage = "An unexpected error occurred.";
			}

			return View("Error", model);
		}
	}
}
