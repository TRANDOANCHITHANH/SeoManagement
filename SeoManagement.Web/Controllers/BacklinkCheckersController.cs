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
			if (!projectId.HasValue)
			{
				return View(new BacklinkCheckViewModel());
			}

			var backlinkResults = await _backlinkResultService.GetByProjectIdAsync(projectId.Value) ?? new List<Backlink>();
			var project = await _projectService.GetByIdAsync(projectId.Value) ?? new SEOProject { ProjectID = projectId.Value };

			var viewModel = new BacklinkCheckViewModel
			{
				ProjectId = projectId.Value,
				ProjectName = project.ProjectName,
				ProjectDescription = project.Description,
				BacklinkResults = backlinkResults.Select(b => new BacklinkResultViewModel
				{
					Url = b.Url,
					TotalBacklinks = b.TotalBacklinks,
					ReferringDomains = b.ReferringDomains ?? 0,
					DofollowBacklinks = b.DofollowBacklinks ?? 0,
					DofollowRefDomains = b.DofollowRefDomains ?? 0,
					BacklinksDetails = b.BacklinksDetails ?? "",
					LastCheckedDate = (DateTime)b.LastCheckedDate
				}).ToList()
			};

			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> CheckBacklinks(string backlinkUrl, int? projectId = null)
		{
			if (!projectId.HasValue)
			{
				TempData["Error"] = "Không tìm thấy ID dự án.";
				return RedirectToAction("Index");
			}

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
				var existingResults = await _backlinkResultService.GetByProjectIdAsync(projectId.Value);
				if (existingResults.Any() && !existingResults.Any(b => b.Url == backlinkUrl))
				{
					_logger.LogWarning("Attempted to check a different URL: {backlinkUrl} for projectId: {projectId}. Only one URL is allowed.", backlinkUrl, projectId);
					return Json(new { success = false, message = "Dự án này chỉ được phép kiểm tra một URL duy nhất. Vui lòng sử dụng URL đã được thiết lập." });
				}

				_logger.LogInformation("Checking backlinks for URL: {Url}", backlinkUrl);
				var (totalBacklinks, referringDomains, dofollowBacklinks, dofollowRefDomains, backlinksDetails) = await _backlinkService.CheckBacklinksAsync(backlinkUrl);
				_logger.LogInformation("Backlink check completed: TotalBacklinks={TotalBacklinks}, ReferringDomains={ReferringDomains}", totalBacklinks, referringDomains);

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

				// Trả về view với dữ liệu mới
				var updatedResults = await _backlinkResultService.GetByProjectIdAsync(projectId.Value) ?? new List<Backlink>();
				var viewModel = new BacklinkCheckViewModel
				{
					ProjectId = projectId.Value,
					ProjectName = project.ProjectName,
					ProjectDescription = project.Description,
					BacklinkResults = updatedResults.Select(b => new BacklinkResultViewModel
					{
						Url = b.Url,
						TotalBacklinks = b.TotalBacklinks,
						ReferringDomains = b.ReferringDomains ?? 0,
						DofollowBacklinks = b.DofollowBacklinks ?? 0,
						DofollowRefDomains = b.DofollowRefDomains ?? 0,
						BacklinksDetails = b.BacklinksDetails ?? "",
						LastCheckedDate = (DateTime)b.LastCheckedDate
					}).ToList()
				};

				return Json(new { success = true, message = "Kiểm tra backlink thành công!", data = viewModel });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi kiểm tra backlink cho URL: {Url}", backlinkUrl);
				return Json(new { success = false, message = "Đã xảy ra lỗi khi kiểm tra backlink: " + ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> BacklinkDetail(int projectId)
		{
			var backlinkResults = await _backlinkResultService.GetByProjectIdAsync(projectId);
			if (backlinkResults == null || !backlinkResults.Any())
			{
				return NotFound("Không tìm thấy dữ liệu backlink cho dự án này.");
			}

			var viewModel = new BacklinkDetailViewModel
			{
				ProjectId = projectId,
				BacklinkResults = backlinkResults.Select(r => new BacklinkResultViewModel
				{
					Url = r.Url,
					LastCheckedDate = (DateTime)r.LastCheckedDate,
					TotalBacklinks = r.TotalBacklinks,
					DofollowBacklinks = r.DofollowBacklinks,
					ReferringDomains = r.ReferringDomains,
					DofollowRefDomains = r.DofollowRefDomains,
					BacklinksDetails = r.BacklinksDetails
				}).ToList()
			};

			return View(viewModel);
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
