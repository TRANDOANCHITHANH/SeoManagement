using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class MonthlySearchVolume
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int KeywordSuggestionId { get; set; }

		[Required]
		public string Month { get; set; }

		[Required]
		public int Year { get; set; }

		[Required]
		public int Searches { get; set; }

		// Navigation property
		[ForeignKey("KeywordSuggestionId")]
		public virtual KeywordSuggestion KeywordSuggestion { get; set; }
	}
}
