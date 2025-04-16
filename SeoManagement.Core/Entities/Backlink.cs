using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Backlink
	{
		[Key]
		public int BacklinkID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(500)]
		public string SourceURL { get; set; }

		[Required]
		[StringLength(500)]
		public string TargetURL { get; set; }

		public float? QualityScore { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
	}
}
