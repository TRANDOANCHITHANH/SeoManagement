namespace SeoManagement.API.Models.Dtos
{
	public class SystemConfigDto
	{
		public int ConfigID { get; set; }
		public string ConfigKey { get; set; }
		public string ConfigValue { get; set; }
		public DateTime LastModified { get; set; }
	}
}
