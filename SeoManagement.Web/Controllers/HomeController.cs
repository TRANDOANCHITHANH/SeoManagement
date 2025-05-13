using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;
using SeoManagement.Web.Models;
using SeoManagement.Web.Models.ViewModels;
using System.Diagnostics;

namespace SeoManagement.Web.Controllers
{
	[Authorize(AuthenticationSchemes = "MainAuth", Policy = "UserOnly")]
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;

		public HomeController(ILogger<HomeController> logger, IConfiguration configuration, HttpClient httpClient)
		{
			_logger = logger;
			_configuration = configuration;
			_httpClient = httpClient;
			_httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"]);
		}

		public async Task<IActionResult> Index(int? categoryId)
		{
			var categoryResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<CategoryViewModel>>("api/categories?pageNumber=1&pageSize=100");
			var categories = categoryResponse?.Items?.Where(c => c.IsActive).ToList() ?? new List<CategoryViewModel>();
			ViewBag.Categories = categories;

			var response = await _httpClient.GetFromJsonAsync<PagedResultViewModel<NewViewModel>>("api/news?pageNumber=1&pageSize=3&isPublished=true");
			if (response == null || !response.Items.Any())
			{
				return View(new PagedResultViewModel<NewViewModel> { Items = new List<NewViewModel>() });
			}

			var configResponse = await _httpClient.GetFromJsonAsync<PagedResultViewModel<SystemConfigViewModel>>("api/systemconfigs?pageNumber=1&pageSize=100");
			var configs = configResponse?.Items?.ToDictionary(c => c.ConfigKey, c => c.ConfigValue) ?? new Dictionary<string, string>();
			ViewBag.Configs = configs;
			ViewBag.SelectedCategoryId = categoryId;
			return View(response);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
