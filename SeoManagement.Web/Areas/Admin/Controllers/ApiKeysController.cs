using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(AuthenticationSchemes = "AdminAuth")]
	public class ApiKeysController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<ApiKeysController> _logger;
		public ApiKeysController(HttpClient httpClient, IConfiguration configuration, ILogger<ApiKeysController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
		{
			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<ApiKeyViewModel>>($"/api/apikeys?pageNumber={pageNumber}&pageSize={pageSize}");
			if (response == null)
			{
				return View(new PagedResultViewModel<ApiKeyViewModel> { Items = new List<ApiKeyViewModel>() });
			}

			var viewModel = new PagedResultViewModel<ApiKeyViewModel>
			{
				Items = response.Items.Select(dto => new ApiKeyViewModel
				{
					Id = dto.Id,
					ServiceName = dto.ServiceName,
					KeyValue = dto.KeyValue,
					IsActive = dto.IsActive,
					CreatedDate = dto.CreatedDate,
					LastUsedDate = dto.LastUsedDate,
					ExpiryDate = dto.ExpiryDate
				}).ToList(),
				PageNumber = response.PageNumber,
				PageSize = response.PageSize,
				TotalItems = response.TotalItems
			};

			return View(viewModel);
		}

		public async Task<IActionResult> Details(int id)
		{
			var response = await _httpClient.GetAsync($"api/apikeys/{id}");
			if (!response.IsSuccessStatusCode)
				return NotFound();

			var apiKey = await response.Content.ReadFromJsonAsync<ApiKeyViewModel>();
			if (apiKey == null)
				return NotFound();

			return View(apiKey);
		}

		public IActionResult Create()
		{
			return View(new ApiKeyViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ApiKeyViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var dto = new ApiKeyViewModel
				{
					ServiceName = viewModel.ServiceName,
					KeyValue = viewModel.KeyValue,
					IsActive = viewModel.IsActive,
					ExpiryDate = viewModel.ExpiryDate
				};

				var response = await _httpClient.PostAsJsonAsync("api/apikeys", dto);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "API key đã được tạo thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo API key: {ErrorContent}", errorContent);
				ModelState.AddModelError("", errorContent);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi tạo API key.");
				ModelState.AddModelError("", "Không thể tạo API key do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(viewModel);
		}

		public async Task<IActionResult> Edit(int id)
		{
			try
			{
				_logger.LogInformation("Fetching API key with ID: {Id}", id);
				var response = await _httpClient.GetAsync($"api/apikeys/{id}");
				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning("Không tìm thấy API key để chỉnh sửa: Status={StatusCode}, Content={Content}", (int)response.StatusCode, errorContent);
					TempData["Error"] = "Không tìm thấy API key để chỉnh sửa.";
					return NotFound();
				}

				var dto = await response.Content.ReadFromJsonAsync<ApiKeyViewModel>();
				if (dto == null)
				{
					_logger.LogWarning("Không thể tải dữ liệu API key với ID: {Id}", id);
					TempData["Error"] = "Không thể tải dữ liệu API key.";
					return NotFound();
				}

				var viewModel = new ApiKeyViewModel
				{
					Id = dto.Id,
					ServiceName = dto.ServiceName,
					KeyValue = dto.KeyValue,
					IsActive = dto.IsActive,
					CreatedDate = dto.CreatedDate,
					LastUsedDate = dto.LastUsedDate,
					ExpiryDate = dto.ExpiryDate
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy API key để chỉnh sửa với ID: {Id}", id);
				TempData["Error"] = "Không thể lấy API key để chỉnh sửa. Vui lòng thử lại sau.";
				return NotFound();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, ApiKeyViewModel viewModel)
		{
			_logger.LogInformation("Received Edit request: id={Id}, ViewModel={@ViewModel}", id, viewModel);

			if (id != viewModel.Id)
			{
				_logger.LogWarning("ID không khớp khi cập nhật API key: Id={Id}, Model.Id={ModelId}", id, viewModel.Id);
				return BadRequest("ID không khớp.");
			}

			if (!ModelState.IsValid)
			{
				_logger.LogWarning("ModelState không hợp lệ: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
				return View(viewModel);
			}

			try
			{
				var dto = new
				{
					Id = viewModel.Id,
					ServiceName = viewModel.ServiceName?.Trim(),
					KeyValue = viewModel.KeyValue?.Trim(),
					IsActive = viewModel.IsActive,
					CreatedDate = viewModel.CreatedDate != default ? viewModel.CreatedDate.ToString("o") : DateTime.UtcNow.ToString("o"),
					ExpiryDate = viewModel.ExpiryDate,
					LastUsedDate = viewModel.LastUsedDate
				};
				var response = await _httpClient.PutAsJsonAsync($"api/apikeys/{id}", dto);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "API key đã được cập nhật thành công.";
					return RedirectToAction(nameof(Index));
				}

				_logger.LogError("Lỗi khi cập nhật API key: Status={StatusCode}, Content={Content}", (int)response.StatusCode, responseContent);
				ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật API key: {responseContent}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Lỗi kết nối đến API khi cập nhật API key với ID: {Id}", id);
				ModelState.AddModelError("", "Không thể kết nối đến server. Vui lòng kiểm tra lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi cập nhật API key với ID: {Id}", id);
				ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
			}

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var response = await _httpClient.DeleteAsync($"api/apikeys/{id}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "API key đã được xóa thành công.";
				return RedirectToAction(nameof(Index));
			}

			var errorContent = await response.Content.ReadAsStringAsync();
			_logger.LogError("Lỗi khi xóa API key: {ErrorContent}", errorContent);
			TempData["Error"] = "Không thể xóa API key. Vui lòng thử lại.";
			return RedirectToAction(nameof(Index));
		}
	}
}
