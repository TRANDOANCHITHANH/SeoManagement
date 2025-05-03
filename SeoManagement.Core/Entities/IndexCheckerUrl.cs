using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class IndexCheckerUrl
	{
		[Key]
		public int UrlID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		[StringLength(500)]
		public string Url { get; set; }

		public bool? IsIndexed { get; set; }
		public DateTime? LastCheckedDate { get; set; }
		public string ErrorMessage { get; set; }

		[ForeignKey("ProjectID")]
		public SEOProject Project { get; set; }
	}
}
