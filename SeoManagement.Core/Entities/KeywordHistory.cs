using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class KeywordHistory
	{
		[Key]
		public int HistoryID { get; set; }

		[Required]
		public int KeywordID { get; set; }

		public int Rank { get; set; }

		public DateTime RecordedDate { get; set; } = DateTime.Now;

		// Navigation properties
		[ForeignKey("KeywordID")]
		public virtual Keyword Keyword { get; set; }
	}
}
