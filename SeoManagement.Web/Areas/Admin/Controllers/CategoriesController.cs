using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(AuthenticationSchemes = "AdminAuth")]
	public class CategoriesController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<CategoriesController> _logger;
		public CategoriesController(HttpClient httpClient, IConfiguration configuration, ILogger<CategoriesController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
		{
			try
			{
				var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<CategoryViewModel>>($"/api/categories?pageNumber={pageNumber}&pageSize={pageSize}");
				if (response == null)
				{
					return View(new PagedResultViewModel<CategoryViewModel> { Items = new List<CategoryViewModel>() });
				}

				var viewModel = new PagedResultViewModel<CategoryViewModel>
				{
					Items = response.Items.Select(dto => new CategoryViewModel
					{
						CategoryId = dto.CategoryId,
						Name = dto.Name,
						Slug = dto.Slug,
						IsActive = dto.IsActive,
						CreatedDate = dto.CreatedDate,
					}).ToList(),
					PageNumber = response.PageNumber,
					PageSize = response.PageSize,
					TotalItems = response.TotalItems
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching categories for Index view.");
				TempData["Error"] = "Không thể tải danh sách danh mục. Vui lòng thử lại sau.";
				return View(new PagedResultViewModel<CategoryViewModel> { Items = new List<CategoryViewModel>() });
			}
		}

		public IActionResult Create()
		{
			return View(new CategoryViewModel());
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CategoryViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var dto = new
				{
					Name = viewModel.Name?.Trim(),
					Slug = viewModel.Slug?.Trim() ?? viewModel.Name?.ToLower().Replace(" ", "-"),
					IsActive = viewModel.IsActive,
					CreatedDate = DateTime.UtcNow.ToString("o"),
					UpdatedDate = (string)null
				};

				var response = await _httpClient.PostAsJsonAsync("api/categories", dto);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Danh mục đã được tạo thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Error creating category: Status={StatusCode}, Content={Content}", (int)response.StatusCode, errorContent);
				ModelState.AddModelError("", errorContent);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "System error while creating category.");
				ModelState.AddModelError("", "Không thể tạo danh mục do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(viewModel);
		}

		public async Task<IActionResult> Edit(int categoryId)
		{
			var response = await _httpClient.GetAsync($"api/categories/{categoryId}");
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				TempData["Error"] = "Không tìm thấy danh mục để chỉnh sửa.";
				return NotFound();
			}

			var dto = await response.Content.ReadFromJsonAsync<CategoryViewModel>();
			if (dto == null)
			{
				TempData["Error"] = "Không thể tải dữ liệu danh mục.";
				return NotFound();
			}

			var viewModel = new CategoryViewModel
			{
				CategoryId = dto.CategoryId,
				Name = dto.Name,
				Slug = dto.Slug,
				IsActive = dto.IsActive,
				CreatedDate = dto.CreatedDate,
			};

			return View(viewModel);

		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int categoryId, CategoryViewModel viewModel)
		{
			if (categoryId != viewModel.CategoryId)
			{
				return BadRequest("ID không khớp.");
			}

			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var dto = new
				{
					CategoryId = viewModel.CategoryId,
					Name = viewModel.Name?.Trim(),
					Slug = viewModel.Slug?.Trim() ?? viewModel.Name?.ToLower().Replace(" ", "-"),
					IsActive = viewModel.IsActive,
					CreatedDate = viewModel.CreatedDate != default ? viewModel.CreatedDate.ToString("o") : DateTime.UtcNow.ToString("o"),
				};

				var response = await _httpClient.PutAsJsonAsync($"api/categories/{categoryId}", dto);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Danh mục đã được cập nhật thành công.";
					return RedirectToAction(nameof(Index));
				}

				_logger.LogError("Error updating category: Status={StatusCode}, Content={Content}", (int)response.StatusCode, responseContent);
				ModelState.AddModelError("", $"{responseContent}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error connecting to API while updating category with ID: {CategoryId}", categoryId);
				ModelState.AddModelError("", "Không thể kết nối đến server. Vui lòng kiểm tra lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "System error while updating category with ID: {CategoryId}", categoryId);
				ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
			}

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int categoryId)
		{
			try
			{
				var response = await _httpClient.DeleteAsync($"api/categories/{categoryId}");
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Danh mục đã được xóa thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Error deleting category: Status={StatusCode}, Content={Content}", (int)response.StatusCode, errorContent);
				TempData["Error"] = "Không thể xóa danh mục. Vui lòng thử lại.";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "System error while deleting category with ID: {CategoryId}", categoryId);
				TempData["Error"] = "Không thể xóa danh mục do lỗi hệ thống. Vui lòng thử lại sau.";
			}

			return RedirectToAction(nameof(Index));
		}
	}
}
