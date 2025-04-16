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

		public int? SearchVolume { get; set; }

		public float? Competition { get; set; }

		public int? CurrentRank { get; set; }

		[StringLength(50)]
		public string SearchIntent { get; set; } // Informational, Navigational, Transactional

		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
		public virtual ICollection<KeywordHistory> KeywordHistories { get; set; }
		public virtual ICollection<Prediction> Predictions { get; set; }

	}
}
