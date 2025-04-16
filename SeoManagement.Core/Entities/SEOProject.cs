using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class SEOProject
	{
		[Key]
		public int ProjectID { get; set; }

		[Required]
		public int UserId { get; set; }

		[Required]
		[StringLength(100)]
		public string ProjectName { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		public DateTime StartDate { get; set; } = DateTime.Now;

		public DateTime? EndDate { get; set; }

		[StringLength(20)]
		public string Status { get; set; } // Active, Completed, Pending

		// Navigation properties
		[ForeignKey("UserId")]
		public virtual ApplicationUser User { get; set; }
		public virtual ICollection<Keyword> Keywords { get; set; }
		public virtual ICollection<Content> Contents { get; set; }
		public virtual ICollection<Backlink> Backlinks { get; set; }
		public virtual ICollection<Report> Reports { get; set; }
		public virtual ICollection<KeywordGroup> KeywordGroups { get; set; }
	}
}
