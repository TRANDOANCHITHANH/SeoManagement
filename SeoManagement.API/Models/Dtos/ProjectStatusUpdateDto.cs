using SeoManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SeoManagement.API.Models.Dtos
{
	public class ProjectStatusUpdateDto
	{
		[Required]
		public ProjectStatus Status { get; set; }
	}
}
