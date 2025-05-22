using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
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

		public HomeController(ILogger<HomeController> logger, IConfiguration configuration, HttpClient httpClient, ISEOPerformanceService sEOPerformanceService, UserManager<ApplicationUser> userManager)
		{
			_logger = logger;
			_configuration = configuration;
			_httpClient = httpClient;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_performanceService = sEOPerformanceService;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index(int? categoryId)
		{
			var user = await _userManager.GetUserAsync(User);

			if (user != null)
			{
				var projectsResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOProjectViewModel>>(
					$"/api/seoprojects?pageNumber=1&pageSize=1000&userId={user.Id}");
				var projectIds = projectsResponse?.Items.Select(p => p.ProjectID).ToList() ?? new List<int>();

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

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
