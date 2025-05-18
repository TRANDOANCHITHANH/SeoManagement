namespace SeoManagement.API.Models.Dtos
{
	public class ApiKeyDto
	{
		public int Id { get; set; }
		public string ServiceName { get; set; }
		public string KeyValue { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? LastUsedDate { get; set; }
		public DateTime? ExpiryDate { get; set; }
	}
}
