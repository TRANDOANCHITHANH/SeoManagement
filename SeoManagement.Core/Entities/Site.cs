namespace SeoManagement.Core.Entities
{
	public class Site
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Url { get; set; }
		public int UserId { get; set; }
		public ApplicationUser User { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
