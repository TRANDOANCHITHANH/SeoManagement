using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class NewViewModel
	{
		public int NewsID { get; set; }

		[Required(ErrorMessage = "Tiêu đề là bắt buộc")]
		[StringLength(200, ErrorMessage = "Tiêu đề không được dài quá 200 ký tự")]
		public string Title { get; set; }

		[Required(ErrorMessage = "Nội dung là bắt buộc")]
		public string Content { get; set; }

		public DateTime CreatedDate { get; set; }

		public bool IsPublished { get; set; }

	}
}
