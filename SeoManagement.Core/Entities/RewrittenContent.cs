using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class RewrittenContent
	{
		[Key]
		public int RewriteID { get; set; }

		[Required]
		public int ContentID { get; set; }

		public string OriginalBody { get; set; }

		public string RewrittenBody { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ContentID")]
		public virtual Content Content { get; set; }
	}
}
