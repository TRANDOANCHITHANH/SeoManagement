namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class ApiKeyViewModel
	{
		public int Id { get; set; }

		public string ServiceName { get; set; }

		public string KeyValue { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime? LastUsedDate { get; set; }

		public DateTime? ExpiryDate { get; set; }
	}
}
