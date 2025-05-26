using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SeoManagement.Core.Interfaces;
using SeoManagement.Web.Areas.Admin.Models.ViewModels;

namespace SeoManagement.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(AuthenticationSchemes = "AdminAuth")]
	public class AccessStatsController : Controller
	{
		private readonly IAccessStatsService _accessStatsService;
		public AccessStatsController(IAccessStatsService accessStatsService)
		{
			_accessStatsService = accessStatsService;
		}

		public async Task<IActionResult> Index(int month = 0, int year = 0)
		{
			if (month == 0 || year == 0)
			{
				month = DateTime.Now.Month;
				year = DateTime.Now.Year;
			}

			var statsDto = await _accessStatsService.GetAccessStatsAsync(month, year);
			var model = new AccessStatsViewModel
			{
				TotalPageViews = statsDto.TotalPageViews,
				Today = statsDto.Today,
				Yesterday = statsDto.Yesterday,
				ThisMonth = statsDto.ThisMonth,
				LastMonth = statsDto.LastMonth,
				SelectedMonth = month,
				SelectedYear = year,
				DailyStats = statsDto.DailyStats.Select(d => new DailyStatViewModel
				{
					AccessDate = new DateTime(year, month, d.DayOfMonth),
					PageViews = d.PageViews
				}).ToList()
			};

			model.Months = Enumerable.Range(1, 12).Select(m => new SelectListItem
			{
				Value = m.ToString(),
				Text = new DateTime(2000, m, 1).ToString("MMMM")
			}).ToList();
			model.Years = Enumerable.Range(DateTime.Now.Year - 5, 10).Select(y => new SelectListItem
			{
				Value = y.ToString(),
				Text = y.ToString()
			}).ToList();

			return View(model);
		}
	}
}
