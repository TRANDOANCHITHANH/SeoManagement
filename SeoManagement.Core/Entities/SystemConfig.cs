using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class SystemConfig
	{
		[Key]
		public int ConfigID { get; set; }

		[Required]
		[StringLength(50)]
		public string ConfigKey { get; set; }

		[StringLength(200)]
		public string ConfigValue { get; set; }

		public DateTime LastModified { get; set; } = DateTime.Now;
	}
}
