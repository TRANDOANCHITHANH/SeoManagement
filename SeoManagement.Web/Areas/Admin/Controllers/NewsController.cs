using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class NewsController : Controller
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<NewsController> _logger;

		public NewsController(HttpClient httpClient, IConfiguration configuration, ILogger<NewsController> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
			_logger = logger;
		}

		public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
		{
			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<NewViewModel>>($"/api/news?pageNumber={pageNumber}&pageSize={pageSize}");
			if (response == null)
			{
				return View(new PagedResultViewModel<NewViewModel> { Items = new List<NewViewModel>() });
			}

			var viewModel = new PagedResultViewModel<NewViewModel>
			{
				Items = response.Items.Select(dto => new NewViewModel
				{
					NewsID = dto.NewsID,
					Title = dto.Title,
					Content = dto.Content,
					CreatedDate = dto.CreatedDate,
					IsPublished = dto.IsPublished
				}).ToList(),
				PageNumber = response.PageNumber,
				PageSize = response.PageSize,
				TotalItems = response.TotalItems
			};

			return View(viewModel);
		}

		public async Task<IActionResult> Details(int newId)
		{
			var response = await _httpClient.GetAsync($"api/news/{newId}");
			if (!response.IsSuccessStatusCode)
				return NotFound();

			var news = await response.Content.ReadFromJsonAsync<NewViewModel>();
			if (news == null)
				return NotFound();

			return View(news);
		}

		public IActionResult Create()
		{
			return View(new NewViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(NewViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			try
			{
				var dto = new NewViewModel
				{
					Title = viewModel.Title,
					Content = viewModel.Content,
					IsPublished = viewModel.IsPublished
				};

				var response = await _httpClient.PostAsJsonAsync("api/news", dto);
				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Tin tức đã được tạo thành công.";
					return RedirectToAction(nameof(Index));
				}

				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Lỗi khi tạo tin tức: {ErrorContent}", errorContent);
				ModelState.AddModelError("", errorContent);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi tạo tin tức.");
				ModelState.AddModelError("", "Không thể tạo tin tức do lỗi hệ thống. Vui lòng thử lại sau.");
			}

			return View(viewModel);
		}

		public async Task<IActionResult> Edit(int newId)
		{
			try
			{
				_logger.LogInformation("Fetching news with ID: {NewId}", newId);
				var response = await _httpClient.GetAsync($"api/news/{newId}");
				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning("Không tìm thấy tin tức để chỉnh sửa: Status={StatusCode}, Content={Content}", (int)response.StatusCode, errorContent);
					TempData["Error"] = "Không tìm thấy tin tức để chỉnh sửa.";
					return NotFound();
				}

				var dto = await response.Content.ReadFromJsonAsync<NewViewModel>();
				if (dto == null)
				{
					_logger.LogWarning("Không thể tải dữ liệu tin tức với ID: {NewId}", newId);
					TempData["Error"] = "Không thể tải dữ liệu tin tức.";
					return NotFound();
				}

				var viewModel = new NewViewModel
				{
					NewsID = dto.NewsID,
					Title = dto.Title,
					Content = dto.Content,
					CreatedDate = dto.CreatedDate,
					IsPublished = dto.IsPublished
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy tin tức để chỉnh sửa với ID: {NewId}", newId);
				TempData["Error"] = "Không thể lấy tin tức để chỉnh sửa. Vui lòng thử lại sau.";
				return NotFound();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int newId, NewViewModel viewModel)
		{
			_logger.LogInformation("Received Edit request: newId={NewId}, ViewModel={@ViewModel}", newId, viewModel);

			if (newId != viewModel.NewsID)
			{
				_logger.LogWarning("ID không khớp khi cập nhật tin tức: NewId={NewId}, Model.NewsID={ModelNewsID}", newId, viewModel.NewsID);
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
					NewsID = viewModel.NewsID,
					Title = viewModel.Title?.Trim(),
					Content = viewModel.Content?.Trim(),
					CreatedDate = viewModel.CreatedDate != default ? viewModel.CreatedDate.ToString("o") : DateTime.UtcNow.ToString("o"),
					IsPublished = viewModel.IsPublished
				};
				var response = await _httpClient.PutAsJsonAsync($"api/news/{newId}", dto);
				var responseContent = await response.Content.ReadAsStringAsync(); // Đọc nội dung response để debug
				_logger.LogInformation("API Response: Status={StatusCode}, Content={Content}", (int)response.StatusCode, responseContent);

				if (response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Tin tức đã được cập nhật thành công.";
					return RedirectToAction(nameof(Index));
				}

				_logger.LogError("Lỗi khi cập nhật tin tức: Status={StatusCode}, Content={Content}", (int)response.StatusCode, responseContent);
				ModelState.AddModelError("", $"Có lỗi xảy ra khi cập nhật tin tức: {responseContent}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Lỗi kết nối đến API khi cập nhật tin tức với ID: {NewId}", newId);
				ModelState.AddModelError("", "Không thể kết nối đến server. Vui lòng kiểm tra lại.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi hệ thống khi cập nhật tin tức với ID: {NewId}", newId);
				ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
			}

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int newId)
		{
			var response = await _httpClient.DeleteAsync($"api/news/{newId}");
			if (response.IsSuccessStatusCode)
			{
				TempData["Success"] = "Tin tức đã được xóa thành công.";
				return RedirectToAction(nameof(Index));
			}

			var errorContent = await response.Content.ReadAsStringAsync();
			_logger.LogError("Lỗi khi xóa tin tức: {ErrorContent}", errorContent);
			TempData["Error"] = "Không thể xóa tin tức. Vui lòng thử lại.";
			return RedirectToAction(nameof(Index));
		}
	}
}
