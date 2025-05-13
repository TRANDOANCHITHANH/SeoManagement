using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class Category
	{
		[Key]
		public int CategoryId { get; set; }

		[Required]
		public string Name { get; set; }

		public string Slug { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
