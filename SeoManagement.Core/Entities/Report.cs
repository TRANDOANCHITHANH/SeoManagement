using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Report
	{
		[Key]
		public int ReportID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(100)]
		public string ReportName { get; set; }

		public DateTime GeneratedDate { get; set; } = DateTime.Now;

		public string Content { get; set; }

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
	}
}
