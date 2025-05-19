using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class UserActionLimit
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int UserId { get; set; }

		[Required]
		public string ActionType { get; set; }

		[Required]
		public int DailyLimit { get; set; } = 10;

		public int ActionsToday { get; set; } = 0;

		[Required]
		public DateTime LastActionDate { get; set; } = DateTime.UtcNow;

		// Navigation property
		public virtual ApplicationUser User { get; set; }
	}
}
