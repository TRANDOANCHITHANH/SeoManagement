using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Areas.Admin.Models.ViewModels
{
	public class CategoryViewModel
	{
		public int CategoryId { get; set; }

		[Required]
		public string Name { get; set; }
		public string Slug { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
