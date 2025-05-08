using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Core.Interfaces;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UsersController : Controller
	{
		private readonly IUserService _userService;
		private readonly HttpClient _httpClient;
		public UsersController(IUserService userService, HttpClient httpClient)
		{
			_userService = userService;
			_httpClient = httpClient;
		}


		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
		{
			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<UserViewModel>>($"https://localhost:7186/api/users?pageNumber={pageNumber}&pageSize={pageSize}");
			return View(response);
		}

		public async Task<IActionResult> Details(int id)
		{
			var user = await _userService.GetByIdAsync(id);
			if (user == null)
				return NotFound();

			var userViewModel = new UserViewModel
			{
				UserID = user.Id,
				Username = user.UserName,
				Email = user.Email,
				FullName = user.FullName,

			};
			return View(userViewModel);
		}

		public async Task<IActionResult> Edit(int id)
		{
			try
			{
				var response = await _httpClient.GetAsync($"https://localhost:7186/api/users/{id}");
				if (!response.IsSuccessStatusCode)
				{
					return NotFound();
				}

				var user = await response.Content.ReadFromJsonAsync<UserViewModel>();
				if (user == null)
				{
					return NotFound();
				}

				return View(user);
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", $"Lỗi khi lấy thông tin người dùng: {ex.Message}");
				return View(new UserViewModel());
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, UserViewModel user)
		{
			if (id != user.UserID)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return View(user);
			}

			var response = await _httpClient.PutAsJsonAsync($"https://localhost:7186/api/users/{id}", user);
			if (response.IsSuccessStatusCode)
			{
				return RedirectToAction(nameof(Index));
			}

			ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật người dùng.");
			return View(user);
		}


		public async Task<IActionResult> Delete(int id)
		{
			var response = await _httpClient.DeleteAsync($"https://localhost:7186/api/users/{id}");
			if (response.IsSuccessStatusCode)
			{
				return RedirectToAction(nameof(Index));
			}

			return NotFound();
		}
	}
}
