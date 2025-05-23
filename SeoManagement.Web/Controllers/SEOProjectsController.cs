using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Entities;
using SeoManagement.Web.Models.ViewModels;
using System.Text.Json;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth")]
	public class SEOProjectsController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<SEOProjectsController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;

		public SEOProjectsController(HttpClient httpClient, IConfiguration configuration, ILogger<SEOProjectsController> logger, UserManager<ApplicationUser> userManager)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 1000, string projectType = null)
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

			var projects = new PagedResultViewModel<SEOProjectViewModel>
			{
				Items = response.Items.Where(p => p.ProjectType == projectType).ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = response.Items.Count(p => p.ProjectType == projectType)
			};

			ViewBag.ProjectType = projectType;
			return View(projects);
		}

		public async Task<IActionResult> Details(int id)
		{
			var response = await _httpClient.GetAsync($"/api/seoprojects/{id}");
			if (!response.IsSuccessStatusCode)
			{
				return NotFound();
			}

			var project = await response.Content.ReadFromJsonAsync<SEOProjectViewModel>();
			if (project == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu dự án.";
				return NotFound();
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null || project.UserID != user.Id)
			{
				_logger.LogWarning("Người dùng {UserId} không có quyền xem dự án {ProjectId}", user?.Id, id);
				TempData["Error"] = "Bạn không có quyền xem dự án này.";
				return Forbid();
			}

			var users = await GetUsers() ?? new List<UserViewModel>();
			ViewBag.UserName = users.FirstOrDefault(u => u.UserID == project.UserID)?.FullName ?? "Không xác định";

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

		public ActionResult Create(string projectType = null)
		{
			return View(new SEOProjectViewModel
			{
				ProjectType = projectType ?? string.Empty,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddDays(30),
				Status = Models.ViewModels.ProjectStatus.Active
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SEOProjectViewModel project)
		{
			if (!ModelState.IsValid)
			{
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
				var projectViewModel = new
				{
					UserID = user.Id,
					ProjectName = project.ProjectName,
					ProjectType = project.ProjectType,
					StartDate = project.StartDate,
					EndDate = project.EndDate,
					Description = project.Description,
					Status = (int)project.Status,
				};

				var response = await _httpClient.PostAsJsonAsync("/api/seoprojects", projectViewModel);
				if (response.IsSuccessStatusCode)
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					var createdProject = JsonSerializer.Deserialize<SEOProjectViewModel>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					if (createdProject != null && createdProject.ProjectID > 0)
					{
						TempData["Success"] = "Tạo dự án thành công.";
						return RedirectToAction(nameof(Index), new { projectType = project.ProjectType });
					}
					else
					{
						_logger.LogError("Không thể lấy ProjectID từ response: {ResponseContent}", responseContent);
						TempData["Error"] = "Không thể lấy ID của dự án mới tạo.";
						return View(project);
					}
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo dự án SEO: {ErrorContent}", errorContent);
				ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo dự án SEO. Vui lòng thử lại");
				return View(project);
			}
			catch (Exception ex)
			{
				_logger.LogError("Lỗi khi tạo dự án SEO");
				ModelState.AddModelError("", $"Lỗi khi tạo dự án SEO: {ex.Message}");
				return View(project);
			}
		}

		public async Task<IActionResult> Edit(int id)
		{
			var response = await _httpClient.GetAsync($"/api/seoprojects/{id}");
			if (!response.IsSuccessStatusCode)
			{
				return NotFound();
			}

			var project = await response.Content.ReadFromJsonAsync<SEOProjectViewModel>();
			if (project == null)
			{
				return NotFound();
			}

			ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
			return View(project);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, SEOProjectViewModel project)
		{
			if (id != project.ProjectID)
			{
				TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng thử lại.";
				return Redirect(Request.Headers["Referer"].ToString() ?? Url.Action(nameof(Index)));
			}

			if (!ModelState.IsValid)
			{
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
				var projectViewModel = new
				{
					ProjectID = project.ProjectID,
					UserID = user.Id,
					ProjectName = project.ProjectName,
					Description = project.Description,
					ProjectType = project.ProjectType,
					StartDate = project.StartDate,
					EndDate = project.EndDate,
					Status = (int)project.Status,
				};

				var response = await _httpClient.PutAsJsonAsync($"/api/seoprojects/{id}", projectViewModel);
				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();
					_logger.LogError("Lỗi khi cập nhật dự án SEO với ID {ProjectId}: {ErrorDetail}", id, errorContent?.Detail ?? "Lỗi không xác định");
					ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật dự án SEO. Vui lòng thử lại.");
					return View(project);
				}

				TempData["Success"] = "Dự án đã được cập nhật thành công.";
				return RedirectToAction(nameof(Index), new { projectType = project.ProjectType });
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi cập nhật dự án SEO: {ex.Message}");
				return View(project);
			}
		}

		public async Task<IActionResult> Delete(int id)
		{
			var refererUrl = Request.Headers["Referer"].ToString();

			var response = await _httpClient.DeleteAsync($"/api/seoprojects/{id}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Xóa dự án thành công";
			}
			else
			{
				TempData["Error"] = "Không thể xóa dự án. Vui lòng thử lại.";
			}

			if (!string.IsNullOrEmpty(refererUrl))
			{
				return Redirect(refererUrl);
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateStatus(int id, [FromForm] Core.Entities.ProjectStatus status)
		{
			try
			{
				var response = await _httpClient.PutAsJsonAsync(
					$"/api/seoprojects/{id}/status",
					new { Status = status }
				);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Trạng thái dự án đã được cập nhật thành công.";
					return RedirectToAction(nameof(Details), new { id });
				}
				return response.IsSuccessStatusCode
					? RedirectToAction(nameof(Details), new { id })
					: BadRequest();
			}
			catch
			{
				return StatusCode(500);
			}
		}
	}
}
