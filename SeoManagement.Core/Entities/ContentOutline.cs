using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class ContentOutline
	{
		[Key]
		public int OutlineID { get; set; }

		[Required]
		public int ContentID { get; set; }

		[StringLength(200)]
		public string OutlineTitle { get; set; }

		public int OutlineLevel { get; set; }

		[StringLength(500)]
		public string OutlineContent { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ContentID")]
		public virtual Content Content { get; set; }
	}
}
