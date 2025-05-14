using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class PageSpeedResult
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(500)]
		public string Url { get; set; }
		public double? LoadTime { get; set; }
		public double? LCP { get; set; }
		public double? FID { get; set; }
		public double? CLS { get; set; }
		public string Suggestions { get; set; }
		public DateTime? LastCheckedDate { get; set; }

		[ForeignKey("ProjectID")]
		public SEOProject Project { get; set; }
	}
}
