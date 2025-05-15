using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class WebsiteInsightsController : Controller
	{
		private readonly IService<WebsiteInsight> _websiteInsightService;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<WebsiteInsightsController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;
		public WebsiteInsightsController(IService<WebsiteInsight> websiteInsightService, HttpClient httpClient, IConfiguration configuration, UserManager<ApplicationUser> userManager, ILogger<WebsiteInsightsController> logger, ISEOProjectService projectService)
		{
			_websiteInsightService = websiteInsightService;
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"]);
			_userManager = userManager;
			_projectService = projectService;
			_logger = logger;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
		{
			var user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
				return RedirectToAction("Login", "Account");
			}

			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOProjectViewModel>>($"/api/seoprojects?pageNumber={pageNumber}&pageSize={pageSize}&userId={user.Id}");
			if (response == null)
			{
				return View(new PagedResultViewModel<SEOProjectViewModel> { Items = new List<SEOProjectViewModel>() });
			}

			var resultProject = new PagedResultViewModel<SEOProjectViewModel>
			{
				Items = response.Items.Where(p => p.ProjectType == "WebsiteInsight").ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => p.ProjectType == "WebsiteInsight")
			};

			return View(resultProject);
		}

		[HttpGet]
		public async Task<IActionResult> IndexChecker(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var previousResults = await _websiteInsightService.GetByProjectIdAsync(projectId.Value);
				ViewBag.Results = previousResults
					.Select(r => new
					{
						Domain = r.Domain,
						GlobalVisits = r.GlobalVisits ?? 0,
						BounceRate = r.BounceRate * 100 ?? 0,
						PagesPerVisit = r.PagesPerVisit ?? 0,
						TimeOnSite = r.TimeOnSite ?? 0,
						SearchTrafficPercentage = r.SearchTrafficPercentage ?? 0,
						DirectTrafficPercentage = r.DirectTrafficPercentage ?? 0,
						ReferralTrafficPercentage = r.ReferralTrafficPercentage ?? 0,
						SocialTrafficPercentage = r.SocialTrafficPercentage ?? 0,
						PaidReferralTrafficPercentage = r.PaidReferralTrafficPercentage ?? 0,
						MailTrafficPercentage = r.MailTrafficPercentage ?? 0,
						TopCountrySharesJson = r.TopCountrySharesJson ?? "",
						IsDataFromGa = r.IsDataFromGa ?? false,
						TopKeywordsJson = r.TopKeywordsJson,
						GlobalRank = r.GlobalRank ?? 0,
						CountryRankCountry = r.CountryRankCountry,
						CountryRankValue = r.CountryRankValue ?? 0,
						CategoryRankCategory = r.CategoryRankCategory,
						CategoryRankValue = r.CategoryRankValue ?? 0
					})
					.ToList();

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
			}

			return View(new WebsiteInsightViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> IndexChecker(WebsiteInsightViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.DomainInput) || model.ProjectId <= 0)
			{
				model.Message = "Vui lòng nhập domain và chọn ProjectId hợp lệ.";
				return View(model);
			}

			try
			{
				var insight = await ((WebsiteInsightService)_websiteInsightService).GetAndSaveWebsiteInsightsAsync(model.DomainInput, model.ProjectId);

				model.Insight = insight;

				var allResults = await _websiteInsightService.GetByProjectIdAsync(model.ProjectId);
				ViewBag.Results = allResults
					.Select(r => new
					{
						Domain = r.Domain,
						GlobalVisits = r.GlobalVisits ?? 0,
						BounceRate = r.BounceRate * 100 ?? 0,
						PagesPerVisit = r.PagesPerVisit ?? 0,
						TimeOnSite = r.TimeOnSite ?? 0,
						SearchTrafficPercentage = r.SearchTrafficPercentage ?? 0,
						DirectTrafficPercentage = r.DirectTrafficPercentage ?? 0,
						ReferralTrafficPercentage = r.ReferralTrafficPercentage ?? 0,
						SocialTrafficPercentage = r.SocialTrafficPercentage ?? 0,
						PaidReferralTrafficPercentage = r.PaidReferralTrafficPercentage ?? 0,
						MailTrafficPercentage = r.MailTrafficPercentage ?? 0,
						TopCountrySharesJson = r.TopCountrySharesJson ?? "",
						IsDataFromGa = r.IsDataFromGa ?? false,
						TopKeywordsJson = r.TopKeywordsJson,
						GlobalRank = r.GlobalRank ?? 0,
						CountryRankCountry = r.CountryRankCountry,
						CountryRankValue = r.CountryRankValue ?? 0,
						CategoryRankCategory = r.CategoryRankCategory,
						CategoryRankValue = r.CategoryRankValue ?? 0
					})
					.DistinctBy(r => r.Domain)
					.ToList();

				var project = await _projectService.GetByIdAsync(model.ProjectId);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
				ViewBag.ProjectId = model.ProjectId;

				TempData["Success"] = "Phân tích thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi phân tích website cho domain: {Domain}", model.DomainInput);
				model.Message = $"Lỗi: {ex.Message}";
				TempData["Error"] = $"Đã xảy ra lỗi khi phân tích website: {ex.Message}";
			}

			return RedirectToAction("IndexChecker", new { model.ProjectId });
		}

		[HttpPost]
		public async Task<IActionResult> DeleteInsight(int projectId, string domain)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var insight = (await _websiteInsightService.GetByProjectIdAsync(projectId))
					.FirstOrDefault(w => w.Domain == domain);
				if (insight == null)
				{
					TempData["Error"] = "Domain không tồn tại trong dự án.";
					return RedirectToAction(nameof(IndexChecker), new { projectId });
				}

				await _websiteInsightService.DeleteAsync(insight.Id);

				TempData["Success"] = "Xóa domain thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa domain: {Domain} trong dự án: {ProjectId}", domain, projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xóa domain: " + ex.Message;
			}

			return RedirectToAction(nameof(IndexChecker), new { projectId });
		}
	}
}
