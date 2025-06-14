namespace SeoManagement.Web.Models.ViewModels
{
	public class BacklinkResultViewModel
	{
		public string Url { get; set; }
		public int? TotalBacklinks { get; set; }
		public int? DofollowBacklinks { get; set; }
		public int? ReferringDomains { get; set; }
		public int? DofollowRefDomains { get; set; }
		public string BacklinksDetails { get; set; }
		public DateTime LastCheckedDate { get; set; }
	}
}
