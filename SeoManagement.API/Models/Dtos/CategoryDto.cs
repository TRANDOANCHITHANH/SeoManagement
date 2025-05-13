namespace SeoManagement.API.Models.Dtos
{
	public class CategoryDto
	{
		public int CategoryId { get; set; }
		public string Name { get; set; }
		public string Slug { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
