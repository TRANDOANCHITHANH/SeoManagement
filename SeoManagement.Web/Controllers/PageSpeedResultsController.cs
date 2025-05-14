using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class PageSpeedResultsController : Controller
	{
		private readonly IPageSpeedResultService _pageSpeedResultService;
		private readonly ISEOProjectService _projectService;
		private readonly ILogger<ToolsController> _logger;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly PageSpeedService _pageSpeedService;

		public PageSpeedResultsController(IPageSpeedResultService pageSpeedResultService, ISEOProjectService projectService, ILogger<ToolsController> logger, HttpClient httpClient, IConfiguration configuration, UserManager<ApplicationUser> userManager, PageSpeedService pageSpeedService)
		{
			_pageSpeedResultService = pageSpeedResultService;
			_projectService = projectService;
			_logger = logger;
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_userManager = userManager;
			_pageSpeedService = pageSpeedService;
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
				Items = response.Items.Where(p => p.ProjectType == "PageSpeedChecker").ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => p.ProjectType == "PageSpeedChecker")
			};

			return View(resultProject);
		}

		[HttpGet]
		public async Task<IActionResult> PageSpeedChecker(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var previousResults = await _pageSpeedResultService.GetByProjectIdAsync(projectId.Value);
				ViewBag.Results = previousResults
			.Select(r => (
				Url: r.Url,
				LoadTime: r.LoadTime ?? 0.0,
				LCP: r.LCP,
				FID: r.FID,
				CLS: r.CLS,
				Suggestions: r.Suggestions ?? "Không có gợi ý",
				LastCheckedDate: r.LastCheckedDate ?? DateTime.MinValue
			))
			.ToList();

				var project = await _projectService.GetByIdAsync(projectId.Value);
				ViewBag.ProjectName = project.ProjectName;
				ViewBag.ProjectDescription = project.Description;
			}
			return View(new SEOProjectViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> PageSpeedChecker(string urls, int? projectId = null, string inputType = "manual", IFormFile excelFile = null)
		{
			List<string> urlList = new List<string>();

			if (inputType == "excel" && excelFile != null && excelFile.Length > 0)
			{
				try
				{
					using (var stream = new MemoryStream())
					{
						await excelFile.CopyToAsync(stream);
						using (var workbook = new XLWorkbook(stream))
						{
							var worksheet = workbook.Worksheets.FirstOrDefault();
							if (worksheet == null)
							{
								TempData["Error"] = "File Excel không hợp lệ hoặc không chứa dữ liệu.";
								return RedirectToAction("PageSpeedChecker", new { projectId });
							}

							int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
							for (int row = 2; row <= lastRow; row++)
							{
								var url = worksheet.Cell(row, 1).GetValue<string>()?.Trim();
								if (!string.IsNullOrWhiteSpace(url))
								{
									urlList.Add(url);
									_logger.LogInformation("Read URL from Excel: {Url}", url);
								}
							}

							if (!urlList.Any())
							{
								TempData["Error"] = "File Excel không chứa URL hợp lệ.";
								return RedirectToAction("PageSpeedChecker", new { projectId });
							}
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi khi đọc file Excel: {Message}", ex.Message);
					TempData["Error"] = "Đã xảy ra lỗi khi đọc file Excel: " + ex.Message;
					return RedirectToAction("PageSpeedChecker", new { projectId });
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(urls))
				{
					TempData["Error"] = "Vui lòng nhập ít nhất một URL để kiểm tra.";
					return RedirectToAction("PageSpeedChecker", new { projectId });
				}

				urlList = urls.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
							  .Select(url => url.Trim())
							  .Where(url => !string.IsNullOrWhiteSpace(url))
							  .Distinct()
							  .ToList();

				if (!urlList.Any())
				{
					TempData["Error"] = "Không có URL hợp lệ để kiểm tra.";
					return RedirectToAction("PageSpeedChecker", new { projectId });
				}
			}

			for (int i = 0; i < urlList.Count; i++)
			{
				var url = urlList[i];
				if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				{
					urlList[i] = "https://" + url;
				}
			}

			try
			{
				var results = new List<(string Url, double LoadTime, double? LCP, double? FID, double? CLS, string Suggestions, DateTime LastCheckedDate)>();

				foreach (var url in urlList)
				{
					try
					{
						var (loadTime, lcp, fid, cls, suggestions) = await _pageSpeedService.CheckPageSpeedAsync(url);
						results.Add((url, loadTime, lcp, fid, cls, suggestions, DateTime.UtcNow));

						if (projectId.HasValue)
						{
							var pageSpeedResult = new PageSpeedResult
							{
								ProjectID = projectId.Value,
								Url = url,
								LoadTime = loadTime,
								LCP = lcp,
								FID = fid,
								CLS = cls,
								Suggestions = suggestions,
								LastCheckedDate = DateTime.UtcNow
							};
							await _pageSpeedResultService.AddAsync(pageSpeedResult);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Lỗi khi kiểm tra tốc độ tải trang cho URL: {Url}", url);
						results.Add((url, 0, null, null, null, "Không thể kiểm tra: " + ex.Message, DateTime.UtcNow));

						if (projectId.HasValue)
						{
							var pageSpeedResult = new PageSpeedResult
							{
								ProjectID = projectId.Value,
								Url = url,
								LoadTime = 0,
								LCP = null,
								FID = null,
								CLS = null,
								Suggestions = "Không thể kiểm tra: " + ex.Message,
								LastCheckedDate = DateTime.UtcNow
							};
							await _pageSpeedResultService.AddAsync(pageSpeedResult);
						}
					}
					await Task.Delay(100);
				}

				TempData["Success"] = "Thêm URL thành công!";

				if (projectId.HasValue)
				{
					var allResults = await _pageSpeedResultService.GetByProjectIdAsync(projectId.Value);
					var combinedResults = allResults
				.Select(r => (
					Url: r.Url,
					LoadTime: r.LoadTime ?? 0.0,
					LCP: r.LCP,
					FID: r.FID,
					CLS: r.CLS,
					Suggestions: r.Suggestions ?? "Không có gợi ý",
					LastCheckedDate: r.LastCheckedDate ?? DateTime.MinValue
				))
				.DistinctBy(r => r.Url)
				.ToList();
					ViewBag.Results = combinedResults;

					return RedirectToAction("PageSpeedChecker", new { projectId });
				}
				return RedirectToAction("PageSpeedChecker");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi chung khi kiểm tra tốc độ tải trang cho danh sách URL.");
				TempData["Error"] = "Đã xảy ra lỗi khi kiểm tra tốc độ tải trang: " + ex.Message;
				return RedirectToAction("PageSpeedChecker", new { projectId });
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteUrl(int projectId, string url)
		{
			try
			{
				var user = await _userManager.GetUserAsync(User);
				if (user == null)
				{
					TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
					return RedirectToAction("Login", "Account");
				}

				var result = (await _pageSpeedResultService.GetByProjectIdAsync(projectId))
					.FirstOrDefault(u => u.Url == url);
				if (result == null)
				{
					TempData["Error"] = "URL không tồn tại trong dự án.";
					return RedirectToAction(nameof(PageSpeedChecker), new { projectId });
				}

				await _pageSpeedResultService.DeleteAsync(result.Id);

				TempData["Success"] = "Xóa URL thành công!";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi xóa URL: {Url} trong dự án: {ProjectId}", url, projectId);
				TempData["Error"] = "Đã xảy ra lỗi khi xóa URL: " + ex.Message;
			}

			return RedirectToAction(nameof(PageSpeedChecker), new { projectId });
		}
	}
}
