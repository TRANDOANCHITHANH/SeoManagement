using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	public class ToolsController : Controller
	{
		private readonly GoogleCustomSearchService _searchService;
		private readonly IIndexCheckerUrlService _indexCheckerUrlService;
		private readonly ILogger<ToolsController> _logger;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly UserManager<ApplicationUser> _userManager;


		public ToolsController(GoogleCustomSearchService searchService, IIndexCheckerUrlService indexCheckerUrlService, ILogger<ToolsController> logger, HttpClient httpClient, IConfiguration configuration, UserManager<ApplicationUser> userManager)
		{
			_searchService = searchService;
			_indexCheckerUrlService = indexCheckerUrlService;
			_logger = logger;
			_userManager = userManager;
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

			var indexCheckerProjects = new PagedResultViewModel<SEOProjectViewModel>
			{
				Items = response.Items.Where(p => p.ProjectType == "IndexChecker").ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => p.ProjectType == "IndexChecker")
			};

			return View(indexCheckerProjects);
		}

		[HttpGet]
		public async Task<IActionResult> IndexChecker(int? projectId = null)
		{
			ViewBag.ProjectId = projectId;
			if (projectId.HasValue)
			{
				var previousResults = await _indexCheckerUrlService.GetByProjectIdAsync(projectId.Value);
				ViewBag.Results = previousResults.Select(r => (r.Url, r.IsIndexed ?? false, r.ErrorMessage)).ToList();
			}
			return View(new SEOProjectViewModel());
		}

		[HttpPost]
		public async Task<IActionResult> IndexChecker(string urls, int? projectId = null, string inputType = "manual", IFormFile excelFile = null)
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
								return View(new SEOProjectViewModel());
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
								return View(new SEOProjectViewModel());
							}
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi khi đọc file Excel: {Message}", ex.Message);
					TempData["Error"] = "Đã xảy ra lỗi khi đọc file Excel: " + ex.Message;
					return View(new SEOProjectViewModel());
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(urls))
				{
					TempData["Error"] = "Vui lòng nhập ít nhất một URL để kiểm tra.";
					return View(new SEOProjectViewModel());
				}

				urlList = urls.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
							  .Select(url => url.Trim())
							  .Where(url => !string.IsNullOrWhiteSpace(url))
							  .Distinct()
							  .ToList();

				if (!urlList.Any())
				{
					TempData["Error"] = "Không có URL hợp lệ để kiểm tra.";
					return View(new SEOProjectViewModel());
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
				var results = new List<(string Url, bool IsIndexed, string Error)>();
				foreach (var url in urlList)
				{
					try
					{
						bool isIndexed = await _searchService.CheckIfIndexedAsync(url);
						results.Add((url, isIndexed, ""));

						if (projectId.HasValue)
						{
							var indexCheckerUrl = new IndexCheckerUrl
							{
								ProjectID = projectId.Value,
								Url = url,
								IsIndexed = isIndexed,
								LastCheckedDate = DateTime.UtcNow,
								ErrorMessage = ""
							};
							await _indexCheckerUrlService.AddAsync(indexCheckerUrl);
						}

						TempData["Success"] = "Thêm URL thành công!";
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Lỗi khi kiểm tra index cho URL: {Url}", url);
						results.Add((url, false, "Không thể kiểm tra: " + ex.Message));

						if (projectId.HasValue)
						{
							var indexCheckerUrl = new IndexCheckerUrl
							{
								ProjectID = projectId.Value,
								Url = url,
								IsIndexed = false,
								LastCheckedDate = DateTime.UtcNow,
								ErrorMessage = "Không thể kiểm tra: " + ex.Message
							};
							await _indexCheckerUrlService.AddAsync(indexCheckerUrl);
						}
					}
					await Task.Delay(100);
				}
				if (projectId.HasValue)
				{
					var allResults = await _indexCheckerUrlService.GetByProjectIdAsync(projectId.Value);
					var combinedResults = allResults
						.Select(r => (r.Url, r.IsIndexed ?? false, r.ErrorMessage))
						.Concat(results)
						.DistinctBy(r => r.Url)
						.ToList();
					ViewBag.Results = combinedResults;
				}
				else
				{
					ViewBag.Results = results;
				}
				ViewBag.Urls = string.Join("\n", urlList);

				ViewBag.ProjectId = projectId;
				return View(new SEOProjectViewModel());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi chung khi kiểm tra index cho danh sách URL.");
				TempData["Error"] = "Đã xảy ra lỗi khi kiểm tra index: " + ex.Message;
				return View(new SEOProjectViewModel());
			}
		}


		public async Task<List<UserViewModel>> GetUsers()
		{
			var response = await _httpClient.GetAsync("/api/users?pageNumber=1&pageSize=1000");
			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"API call failed with status: {response.StatusCode}");
				return new List<UserViewModel>();
			}

			var result = await response.Content.ReadFromJsonAsync<PagedResultViewModel<UserViewModel>>();
			return result?.Items ?? new List<UserViewModel>();
		}
	}
}