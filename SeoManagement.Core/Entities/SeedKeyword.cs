using SeoManagement.Core.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class SeedKeyword : KeywordBase
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(100)]
		public string Keyword { get; set; }

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }

		public virtual ICollection<RelatedKeyword> RelatedKeywords { get; set; } = new List<RelatedKeyword>();
	}
}
