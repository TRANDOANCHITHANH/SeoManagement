using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Core.Interfaces;
using SeoManagement.Infrastructure.Services;
using SeoManagement.Web.Models.ViewModels;
using System.Text.Json;

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
		public async Task<IActionResult> IndexChecker(string urls, int? projectId = null)
		{
			if (string.IsNullOrWhiteSpace(urls))
			{
				TempData["Error"] = "Vui lòng nhập ít nhất một URL để kiểm tra.";
				return View(new SEOProjectViewModel());
			}

			var urlList = urls.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
							 .Select(url => url.Trim())
							 .Where(url => !string.IsNullOrWhiteSpace(url))
							 .Distinct()
							 .ToList();

			if (!urlList.Any())
			{
				TempData["Error"] = "Không có URL hợp lệ để kiểm tra.";
				return View(new SEOProjectViewModel());
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
						results.Add((url, isIndexed, null));

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

				// Optionally, you can save the results to the project (e.g., store in DB or log them)
				ViewBag.Urls = urls;
				ViewBag.Results = results;
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

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SEOProjectViewModel project)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
				return View(project);
			}


			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				TempData["Error"] = "Không thể xác định thông tin người dùng. Vui lòng đăng nhập lại.";
				return RedirectToAction("Login", "Account");
			}

			if (project.EndDate < project.StartDate)
			{
				ModelState.AddModelError("EndDate", "Ngày kết thúc không thể trước ngày bắt đầu");
				ViewBag.Users = await GetUsers();
				return View(project);
			}

			try
			{
				var response = await _httpClient.PostAsJsonAsync("/api/seoprojects", project);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Tạo dự án thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo dự án SEO: {ErrorContent}", errorContent);
				ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo dự án SEO. Vui lòng thử lại");
			}
			catch (Exception ex)
			{
				_logger.LogError("Lỗi khi tạo dự án SEO");
				ModelState.AddModelError("", $"Lỗi khi tạo dự án SEO: {ex.Message}");
			}

			ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
			return View(project);
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

		[HttpGet]
		public IActionResult CreateIndexCheckerProject()
		{
			return View(new SEOProjectViewModel
			{
				ProjectType = "IndexChecker",
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddDays(30),
				Status = Models.ViewModels.ProjectStatus.Active
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateIndexCheckerProject(SEOProjectViewModel project)
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
						TempData["Success"] = "Dự án đã được tạo thành công!";
						return RedirectToAction("IndexChecker", new { projectId });
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
					_logger.LogError("Lỗi khi tạo dự án SEO: {ErrorContent}", errorContent);
					TempData["Error"] = "Không thể tạo dự án. Vui lòng thử lại.";
					return View(project);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tạo SEO Project.");
				TempData["Error"] = "Đã xảy ra lỗi khi tạo dự án: " + ex.Message;
				return View(project);
			}
		}
	}
}