using SeoManagement.Core.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class RelatedKeyword : KeywordBase
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int SeedKeywordId { get; set; }

		[Required]
		[StringLength(100)]
		public string SuggestedKeyword { get; set; }

		// Navigation properties
		[ForeignKey("SeedKeywordId")]
		public virtual SeedKeyword SeedKeyword { get; set; }
	}
}
