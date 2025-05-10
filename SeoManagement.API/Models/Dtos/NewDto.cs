namespace SeoManagement.API.Models.Dtos
{
	public class NewDto
	{
		public int NewsID { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsPublished { get; set; }
	}
}
