using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class ApplicationUser : IdentityUser<int>
	{
		[Required]
		[MaxLength(100)]
		public string FullName { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		[Required]
		public bool IsActive { get; set; } = true;

		public int DailyKeywordCheckLimit { get; set; } = 5;
		public int KeywordChecksToday { get; set; } = 0;
		public DateTime LastCheckDate { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public virtual ICollection<SEOProject> SEOProjects { get; set; }
		public virtual ICollection<Guide> Guides { get; set; }
		public List<Site> Sites { get; set; } = new List<Site>();
	}
}
