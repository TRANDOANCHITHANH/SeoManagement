using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class Prediction
	{
		[Key]
		public int PredictionID { get; set; }

		[Required]
		public int KeywordID { get; set; }

		public float PredictedRank { get; set; }

		public float Confidence { get; set; }

		public string InputFactors { get; set; } // JSON format

		public DateTime PredictionDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("KeywordID")]
		public virtual Keyword Keyword { get; set; }
	}
}
