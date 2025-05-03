using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Models.ViewModels
{
	public enum ProjectStatus { Active = 0, Completed = 1, Pending = 2 }
	public class SEOProjectViewModel
	{
		public int ProjectID { get; set; }

		[Required]
		//[Range(1, int.MaxValue)]
		public int UserID { get; set; }

		[Required]
		[StringLength(100, MinimumLength = 5)]
		public string ProjectName { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		public string ProjectType { get; set; }

		[DataType(DataType.DateTime)]
		public DateTime StartDate { get; set; } = DateTime.Now;

		[DataType(DataType.DateTime)]
		public DateTime? EndDate { get; set; }

		[Required]
		public ProjectStatus Status { get; set; }

	}
}
