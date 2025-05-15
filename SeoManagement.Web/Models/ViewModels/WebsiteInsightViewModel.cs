using SeoManagement.Core.Entities;

namespace SeoManagement.Web.Models.ViewModels
{
	public class WebsiteInsightViewModel
	{
		public WebsiteInsight Insight { get; set; }
		public string DomainInput { get; set; }
		public int ProjectId { get; set; }
		public string Message { get; set; }
	}
}
