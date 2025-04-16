using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Content
	{
		[Key]
		public int ContentID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(200)]
		public string Title { get; set; }

		[Required]
		public string Body { get; set; }

		[StringLength(100)]
		public string MainKeyword { get; set; }

		public int? WordCount { get; set; }

		[StringLength(20)]
		public string IndexStatus { get; set; } // Indexed, Not Indexed, Pending

		public bool CannibalizationFlag { get; set; } = false;

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		public DateTime LastModified { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
		public virtual ICollection<ContentOutline> ContentOutlines { get; set; }
		public virtual ICollection<RewrittenContent> RewrittenContents { get; set; }
	}
}
