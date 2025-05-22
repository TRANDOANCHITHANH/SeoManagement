using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Data;
using SeoManagement.Web.Models.ViewModels;
using System.Security.Claims;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class SEOPerformancesController : Controller
	{
		private readonly ISEOPerformanceService _performanceService;
		private readonly AppDbContext _context;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<SEOPerformancesController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;

		public SEOPerformancesController(
			ISEOPerformanceService performanceService,
			AppDbContext context,
			HttpClient httpClient,
			IConfiguration configuration,
			ILogger<SEOPerformancesController> logger,
			UserManager<ApplicationUser> userManager)
		{
			_performanceService = performanceService;
			_context = context;
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
			_userManager = userManager;
		}

		[HttpGet]
		public async Task<IActionResult> SelectProject(int pageNumber = 1, int pageSize = 10, string projectType = null)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
				return RedirectToAction("Login", "Account");
			}

			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOProjectViewModel>>(
				$"/api/seoprojects?pageNumber={pageNumber}&pageSize={pageSize}&userId={user.Id}");
			if (response == null)
			{
				return View(new PagedResultViewModel<SEOProjectViewModel> { Items = new List<SEOProjectViewModel>() });
			}

			var projects = new PagedResultViewModel<ProjectSelectionViewModel>
			{
				Items = response.Items
					.Where(p => string.IsNullOrEmpty(projectType) || p.ProjectType == projectType)
					.Select(p => new ProjectSelectionViewModel
					{
						ProjectId = p.ProjectID,
						ProjectName = p.ProjectName,
						ProjectType = p.ProjectType,
						StartDate = p.StartDate
					})
					.ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => string.IsNullOrEmpty(projectType) || p.ProjectType == projectType)
			};

			var projectTypes = new List<SelectListItem>
			{
				new SelectListItem { Value = "", Text = "Tất cả loại dự án" },
				new SelectListItem { Value = "KeywordRankChecker", Text = "Kiểm tra thứ hạng Keyword" },
				new SelectListItem { Value = "SEOOnPage", Text = "Kiểm tra On-Page" },
				new SelectListItem { Value = "PageSpeedChecker", Text = "Phân tích tốc độ Page" },
				new SelectListItem { Value = "BacklinkChecker", Text = "Kiểm tra Backlink" },
				new SelectListItem { Value = "IndexChecker", Text = "Kiểm tra Index" }
			};
			ViewBag.ProjectTypes = new SelectList(projectTypes, "Value", "Text", projectType);
			return View(projects);
		}

		[HttpGet]
		public async Task<IActionResult> Index(int projectId)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-1");
			var project = await _context.SEOProjects.FindAsync(projectId);
			if (project == null || project.UserId != userId)
				return Forbid();

			var history = await _performanceService.GetHistoryByProjectIdAsync(projectId);
			var model = new PerformanceDashboardViewModel
			{
				ProjectId = projectId,
				ProjectName = project.ProjectName,
				ProjectType = project.ProjectType,
				StartDate = project.StartDate,
				History = history.Select(h => new PerformanceEntry
				{
					RecordedAt = h.RecordedAt,
					AverageKeywordRank = h.AverageKeywordRank,
					AverageOnPageScore = h.AverageOnPageScore,
					PageSpeedScore = h.PageSpeedScore,
					BacklinkCount = h.BacklinkCount,
					IndexedPageCount = h.IndexedPageCount,
					UnindexedPageCount = h.UnindexedPageCount,
				}).ToList()
			};
			ViewBag.ProjectType = project.ProjectType;
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Record(int projectId)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-1");
			var project = await _context.SEOProjects.FindAsync(projectId);
			if (project == null || project.UserId != userId)
				return Forbid();

			await _performanceService.RecordPerformanceAsync(projectId);
			TempData["Success"] = "Hiệu suất đã được ghi nhận.";
			return RedirectToAction("Index", new { projectId });
		}
	}
}
