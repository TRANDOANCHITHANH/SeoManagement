using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class ApiKeyViewModel
	{
		public int Id { get; set; }

		[Display(Name = "Dịch vụ")]
		public string ServiceName { get; set; }

		[Display(Name = "Giá trị Key")]
		public string KeyValue { get; set; }

		[Display(Name = "Hoạt động")]
		public bool IsActive { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime? LastUsedDate { get; set; }

		[Display(Name = "Ngày hết hạn")]
		public DateTime? ExpiryDate { get; set; }
	}
}
