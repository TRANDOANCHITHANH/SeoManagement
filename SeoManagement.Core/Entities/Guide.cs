using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Guide
	{
		[Key]
		public int GuideID { get; set; }

		[Required]
		[StringLength(200)]
		public string Title { get; set; }

		[Required]
		public string Body { get; set; }

		[Required]
		public int UserId { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("UserId")]
		public virtual ApplicationUser User { get; set; }
	}
}
