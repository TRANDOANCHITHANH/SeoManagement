using SeoManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SeoManagement.API.Models.Dtos
{
	public class SEOProjectDto
	{
		public int ProjectID { get; set; }

		[Range(1, int.MaxValue)]
		public int UserID { get; set; }

		[Required]
		[StringLength(100, MinimumLength = 5)]
		public string ProjectName { get; set; }

		[StringLength(500)]
		public string Description { get; set; }

		[DataType(DataType.DateTime)]
		public DateTime StartDate { get; set; } = DateTime.Now;

		[DataType(DataType.DateTime)]
		public DateTime? EndDate { get; set; }

		[Required]
		public ProjectStatus Status { get; set; }
	}
}
