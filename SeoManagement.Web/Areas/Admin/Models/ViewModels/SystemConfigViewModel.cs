using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class SystemConfigViewModel
	{
		public int ConfigID { get; set; }

		[Required]
		[StringLength(50, ErrorMessage = "Khóa cấu hình không được vượt quá 50 ký tự.")]
		[Display(Name = "Khóa cấu hình")]
		public string ConfigKey { get; set; }

		[StringLength(200, ErrorMessage = "Giá trị cấu hình không được vượt quá 200 ký tự.")]
		[Display(Name = "Giá trị")]
		public string ConfigValue { get; set; }

		[Display(Name = "Sửa đổi lần cuối")]
		public DateTime LastModified { get; set; }
	}
}
