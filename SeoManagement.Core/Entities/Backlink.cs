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
		public string Url { get; set; }

		public int TotalBacklinks { get; set; }

		public int? ReferringDomains { get; set; }

		public int? DofollowBacklinks { get; set; }
		public int? DofollowRefDomains { get; set; }

		public DateTime? LastCheckedDate { get; set; }
		public string BacklinksDetails { get; set; }
		// Navigation properties
		[ForeignKey("ProjectID")]
		public SEOProject Project { get; set; }
	}
}
