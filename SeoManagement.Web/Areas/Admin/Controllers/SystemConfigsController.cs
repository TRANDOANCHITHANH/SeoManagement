using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(AuthenticationSchemes = "AdminAuth")]
	public class SystemConfigsController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<SystemConfigsController> _logger;

		public SystemConfigsController(HttpClient httpClient, IConfiguration configuration, ILogger<SystemConfigsController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
		{
			try
			{
				var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SystemConfigViewModel>>($"api/systemconfigs?pageNumber={pageNumber}&pageSize={pageSize}");
				if (response == null || !response.Items.Any())
				{
					return View(new PagedResultViewModel<SystemConfigViewModel> { Items = new List<SystemConfigViewModel>() });
				}
				return View(response);
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Không thể tải danh sách cấu hình. Vui lòng thử lại sau.";
				return View(new PagedResultViewModel<SystemConfigViewModel> { Items = new List<SystemConfigViewModel>() });
			}
		}

		public IActionResult Create()
		{
			return View(new SystemConfigViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SystemConfigViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/systemconfigs", viewModel);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Cấu hình đã được tạo thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo cấu hình: {ErrorContent}", errorContent);
				ModelState.AddModelError("", "Có lỗi xảy ra khi tạo cấu hình. Vui lòng thử lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi tạo cấu hình.");
				ModelState.AddModelError("", "Không thể tạo cấu hình do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(viewModel);
		}

		public async Task<IActionResult> Edit(int configId)
		{
			var config = await _httpClient.GetFromJsonAsync<SystemConfigViewModel>($"api/systemconfigs/{configId}");
			if (config == null)
			{
				return NotFound();
			}
			return View(config);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int configId, SystemConfigViewModel viewModel)
		{
			if (configId != viewModel.ConfigID)
			{
				return BadRequest("ID không khớp.");
			}

			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var response = await _httpClient.PutAsJsonAsync($"api/systemconfigs/{configId}", viewModel);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Cấu hình đã được cập nhật thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi cập nhật cấu hình: {ErrorContent}", errorContent);
				ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật cấu hình. Vui lòng thử lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi cập nhật cấu hình.");
				ModelState.AddModelError("", "Không thể cập nhật cấu hình do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int configId)
		{
			var response = await _httpClient.DeleteAsync($"api/systemconfigs/{configId}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Cấu hình đã được xóa thành công.";
			}
			else
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				TempData["Error"] = "Có lỗi xảy ra khi xóa cấu hình. Vui lòng thử lại.";
			}
			return RedirectToAction(nameof(Index));
		}
	}
}
