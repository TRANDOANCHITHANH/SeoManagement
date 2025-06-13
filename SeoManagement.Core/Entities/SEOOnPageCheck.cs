using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class SEOOnPageCheck
	{
		[Key]
		public int CheckID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		public SEOProject Project { get; set; }

		[Required]
		[StringLength(500)]
		public string Url { get; set; }

		[StringLength(100)]
		public string MainKeyword { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
