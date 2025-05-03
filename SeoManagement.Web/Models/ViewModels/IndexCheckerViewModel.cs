namespace SeoManagement.Web.Models.ViewModels
{
	public class IndexCheckerViewModel
	{
		public int UrlID { get; set; }
		public string Url { get; set; }
		public bool? IsIndexed { get; set; }
		public DateTime? LastCheckedDate { get; set; }
		public string ErrorMessage { get; set; }
	}
}
