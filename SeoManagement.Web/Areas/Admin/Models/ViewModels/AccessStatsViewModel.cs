using Microsoft.AspNetCore.Mvc.Rendering;

namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class AccessStatsViewModel
	{
		public int TotalPageViews { get; set; }
		public int Today { get; set; }
		public int Yesterday { get; set; }
		public int ThisMonth { get; set; }
		public int LastMonth { get; set; }
		public int SelectedMonth { get; set; }
		public int SelectedYear { get; set; }
		public List<DailyStatViewModel> DailyStats { get; set; }

		public List<SelectListItem> Months { get; set; }
		public List<SelectListItem> Years { get; set; }
	}

	public class DailyStatViewModel
	{
		public DateTime AccessDate { get; set; }
		public int PageViews { get; set; }
	}
}
