using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Keyword
	{
		[Key]
		public int KeywordID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(100)]
		public string KeywordName { get; set; }

		[StringLength(500)]
		public string Domain { get; set; }

		public int? TopVolume { get; set; }

		public int? TopPosition { get; set; }

		[StringLength(4000)]
		public string SerpResultsJson { get; set; } = string.Empty;

		public DateTime LastUpdate { get; set; }

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }

		public virtual ICollection<KeywordHistory> KeywordHistories { get; set; }

	}
}
