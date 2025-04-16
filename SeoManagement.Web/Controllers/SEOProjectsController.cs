using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Controllers
{
	public class SEOProjectsController : Controller
	{
		private readonly HttpClient _httpClient;
		public SEOProjectsController(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
		{
			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SEOProjectViewModel>>($"https://localhost:7186/api/seoprojects?pageNumber={pageNumber}&pageSize={pageSize}");
			if (response == null)
			{
				return View(new PagedResultViewModel<SEOProjectViewModel> { Items = new List<SEOProjectViewModel>() });
			}
			return View(response);
		}

		public async Task<IActionResult> Details(int id)
		{
			var response = await _httpClient.GetAsync($"https://localhost:7186/api/seoprojects/{id}");
			if (!response.IsSuccessStatusCode)
			{
				return NotFound();
			}

			var project = await response.Content.ReadFromJsonAsync<SEOProjectViewModel>();
			if (project == null)
			{
				return NotFound();
			}

			var users = await GetUsers() ?? new List<UserViewModel>();
			ViewBag.UserName = users.FirstOrDefault(u => u.UserID == project.UserID)?.FullName ?? "Không xác định";

			return View(project);
		}

		public async Task<List<UserViewModel>> GetUsers()
		{
			var response = await _httpClient.GetAsync("https://localhost:7186/api/users?pageNumber=1&pageSize=1000");
			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"API call failed with status: {response.StatusCode}");
				return new List<UserViewModel>();
			}

			var result = await response.Content.ReadFromJsonAsync<PagedResultViewModel<UserViewModel>>();
			return result?.Items ?? new List<UserViewModel>();
		}

		public async Task<IActionResult> Create()
		{
			ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
			return View(new SEOProjectViewModel());
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

			try
			{
				var response = await _httpClient.PostAsJsonAsync("https://localhost:7186/api/seoprojects", project);
				if (response.IsSuccessStatusCode)
				{
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo dự án SEO: {response.StatusCode} - {errorContent}");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi tạo dự án SEO: {ex.Message}");
			}

			ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
			return View(project);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var response = await _httpClient.GetAsync($"https://localhost:7186/api/seoprojects/{id}");
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
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				ViewBag.Users = await GetUsers() ?? new List<UserViewModel>();
				return View(project);
			}

			try
			{
				var response = await _httpClient.PutAsJsonAsync($"https://localhost:7186/api/seoprojects/{id}", project);
				if (response.IsSuccessStatusCode)
				{
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật dự án SEO: {response.StatusCode} - {errorContent}");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi cập nhật dự án SEO: {ex.Message}");
			}

			ViewBag.Users = await GetUsers() ?? new List<UserViewModel>(); // Đảm bảo không null
			return View(project);
		}

		public async Task<IActionResult> Delete(int id)
		{
			var response = await _httpClient.DeleteAsync($"https://localhost:7186/api/seoprojects/{id}");
			if (response.IsSuccessStatusCode)
			{
				return RedirectToAction(nameof(Index));
			}

			return NotFound();
		}


	}
}
