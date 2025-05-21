using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class KeywordSuggestion
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		public string SeedKeyword { get; set; }

		[Required]
		public string SuggestedKeyword { get; set; }

		public bool IsMainKeyword { get; set; }

		public int? SearchVolume { get; set; }

		public int? Difficulty { get; set; }

		public decimal? CPC { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		// Navigation property
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }

		public virtual ICollection<MonthlySearchVolume> MonthlySearchVolumes { get; set; }
	}
}
