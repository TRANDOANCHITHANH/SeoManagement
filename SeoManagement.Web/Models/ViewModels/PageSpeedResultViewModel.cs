namespace SeoManagement.Web.Models.ViewModels
{
	public class PageSpeedResultViewModel
	{
		public int Id { get; set; }
		public int ProjectID { get; set; }
		public string Url { get; set; }
		public double? LoadTime { get; set; }
		public double? LCP { get; set; }
		public double? FID { get; set; }
		public double? CLS { get; set; }
		public string Suggestions { get; set; }
		public DateTime? LastCheckedDate { get; set; }
	}
}
