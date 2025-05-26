using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class AccessStatistics
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime Time { get; set; }

		[Required]
		public int VisitorCounter { get; set; }
	}
}
