using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class KeywordGroup
	{
		[Key]
		public int GroupID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[StringLength(100)]
		public string GroupName { get; set; }

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
	}
}
