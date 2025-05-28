using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class AlertConfiguration
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectId { get; set; }

		public bool IsAlertMonitored { get; set; }

		[ForeignKey("ProjectId")]
		public virtual SEOProject Project { get; set; }
	}
}
