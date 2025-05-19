using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Enum;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;


namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class BacklinkCheckersController : Controller
	{
		private readonly IBacklinkResultService _backlinkResultService;
		private readonly IUserService _userService;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<BacklinkCheckersController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly BacklinkService _backlinkService;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;

		public BacklinkCheckersController(
			IBacklinkResultService backlinkResultService,
			IUserService userService,
			ISEOProjectService projectService,
			ILogger<BacklinkCheckersController> logger,
			UserManager<ApplicationUser> userManager,
			BacklinkService backlinkService,
			HttpClient httpClient,
			IConfiguration configuration)
		{
			_backlinkResultService = backlinkResultService;
			_userService = userService;
			_projectService = projectService;
			_logger = logger;
			_userManager = userManager;
			_backlinkService = backlinkService;
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
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
				Items = response.Items.Where(p => p.ProjectType == "BacklinkChecker").ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => p.ProjectType == "BacklinkChecker")
			};

			return View(resultProject);
		}

		[HttpGet]
		public async Task<IActionResult> CheckBacklinks(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			ViewBag.BacklinkResults = new List<(string Url, int TotalBacklinks, int ReferringDomains, int DofollowBacklinks, int DofollowRefDomains, string BacklinksDetails, DateTime LastCheckedDate)>();
			if (projectId.HasValue)
			{
				var backlinkResults = await _backlinkResultService.GetByProjectIdAsync(projectId.Value);
				if (backlinkResults == null)
				{
					_logger.LogWarning("Backlink results returned null for ProjectID: {ProjectId}", projectId.Value);
				}
				else
				{
					_logger.LogInformation("Retrieved backlink results for ProjectID {ProjectId}: {Count} items", projectId.Value, backlinkResults.Count);
					ViewBag.BacklinkResults = backlinkResults
						.Select(b => (
							Url: b.Url,
							TotalBacklinks: b.TotalBacklinks,
							ReferringDomains: b.ReferringDomains,
							DofollowBacklinks: b.DofollowBacklinks,
							DofollowRefDomains: b.DofollowRefDomains,
							BacklinksDetails: b.BacklinksDetails,
							LastCheckedDate: b.LastCheckedDate
						))
						.ToList();

					_logger.LogInformation("ViewBag.BacklinkResults assigned: {Count} items", (ViewBag.BacklinkResults as List<(string, int, int, int, int, string, DateTime)>)?.Count ?? 0);
				}

				var project = await _projectService.GetByIdAsync(projectId.Value);
				if (project == null)
				{
					_logger.LogWarning("Project not found for ProjectID: {ProjectId}", projectId.Value);
					ViewBag.ProjectName = "N/A";
					ViewBag.ProjectDescription = "N/A";
				}
				else
				{
					ViewBag.ProjectName = project.ProjectName;
					ViewBag.ProjectDescription = project.Description;
				}
			}
			return View(new SEOProjectViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> CheckBacklinks(string backlinkUrl, int? projectId = null)
		{
			if (string.IsNullOrWhiteSpace(backlinkUrl))
			{
				TempData["Error"] = "Vui lòng nhập URL để kiểm tra backlink.";
				return RedirectToAction("CheckBacklinks", new { projectId });
			}

			if (!backlinkUrl.StartsWith("http://") && !backlinkUrl.StartsWith("https://"))
			{
				backlinkUrl = "https://" + backlinkUrl;
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login", "Account");
			}
			var project = await _projectService.GetByIdAsync(projectId.Value);
			if (!await _userService.CanPerformActionAsync(user.Id, ActionType.BacklinkChecker.ToString()))
			{
				TempData["Error"] = "Bạn đã vượt quá giới hạn kiểm tra backlink mỗi ngày.";
				return RedirectToAction("CheckBacklinks", new { projectId = project.ProjectID });
			}
			try
			{
				_logger.LogInformation("Checking backlinks for URL: {Url}", backlinkUrl);
				var (totalBacklinks, referringDomains, dofollowBacklinks, dofollowRefDomains, backlinksDetails) = await _backlinkService.CheckBacklinksAsync(backlinkUrl);
				_logger.LogInformation("Backlink check completed: TotalBacklinks={TotalBacklinks}, ReferringDomains={ReferringDomains}", totalBacklinks, referringDomains);

				if (projectId.HasValue)
				{
					var backlinkResult = new Backlink
					{
						ProjectID = projectId.Value,
						Url = backlinkUrl,
						TotalBacklinks = totalBacklinks,
						ReferringDomains = referringDomains,
						DofollowBacklinks = dofollowBacklinks,
						DofollowRefDomains = dofollowRefDomains,
						BacklinksDetails = backlinksDetails,
						LastCheckedDate = DateTime.UtcNow
					};

					_logger.LogInformation("Attempting to add Backlink: {@Backlink}", backlinkResult);
					await _backlinkResultService.AddAsync(backlinkResult);
					_logger.LogInformation("Backlink added successfully for URL: {Url}", backlinkUrl);
					await _userService.IncrementActionCountAsync(user.Id, ActionType.BacklinkChecker.ToString());

					var updatedResults = await _backlinkResultService.GetByProjectIdAsync(projectId.Value);
					if (updatedResults == null || !updatedResults.Any())
					{
						_logger.LogWarning("No backlink results retrieved for ProjectID: {ProjectId} after adding", projectId.Value);
					}
					else
					{
						_logger.LogInformation("Retrieved {Count} backlink results for ProjectID {ProjectId} after adding: {@Results}", updatedResults.Count, projectId.Value, updatedResults);
					}
					ViewBag.BacklinkResults = updatedResults
						.Select(b => (
							Url: b.Url,
							TotalBacklinks: b.TotalBacklinks,
							ReferringDomains: b.ReferringDomains ?? 0,
							DofollowBacklinks: b.DofollowBacklinks ?? 0,
							DofollowRefDomains: b.DofollowRefDomains ?? 0,
							 BacklinksDetails: b.BacklinksDetails ?? "",
							LastCheckedDate: b.LastCheckedDate
						))
						.ToList();

					ViewBag.ProjectId = projectId;
					ViewBag.ProjectName = project.ProjectName;
					ViewBag.ProjectDescription = project.Description;
				}

				TempData["Success"] = "Kiểm tra backlink thành công!";
				return RedirectToAction("CheckBacklinks", new { projectId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi kiểm tra backlink cho URL: {Url}", backlinkUrl);
				TempData["Error"] = "Đã xảy ra lỗi khi kiểm tra backlink: " + ex.Message;
				return RedirectToAction("CheckBacklinks", new { projectId });
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteBacklink(int projectId, string url)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var backlinkResult = (await _backlinkResultService.GetByProjectIdAsync(projectId))
					.FirstOrDefault(b => b.Url == url);
				if (backlinkResult == null)
				{
					TempData["Error"] = "URL không tồn tại trong dự án.";
					return RedirectToAction("Index", new { projectId });
				}

				await _backlinkResultService.DeleteAsync(backlinkResult.BacklinkID);
				TempData["Success"] = "Xóa URL backlink thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa URL backlink: {Url} trong dự án: {ProjectId}", url, projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xóa URL backlink: " + ex.Message;
			}

			return RedirectToAction("Index", new { projectId });
		}
	}
}
