using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Models.ViewModels
{
	public class SiteViewModel
	{
		[Required(ErrorMessage = "Tên website là bắt buộc")]
		public string Name { get; set; }

		[Required(ErrorMessage = "URL website là bắt buộc")]
		[Url(ErrorMessage = "URL không hợp lệ")]
		public string Url { get; set; }
	}
}
