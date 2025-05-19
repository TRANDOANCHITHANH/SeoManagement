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

		// Navigation properties
		public virtual ICollection<SEOProject> SEOProjects { get; set; }
		public virtual ICollection<Guide> Guides { get; set; }
		public List<Site> Sites { get; set; } = new List<Site>();
		public virtual ICollection<UserActionLimit> ActionLimits { get; set; }
	}
}
