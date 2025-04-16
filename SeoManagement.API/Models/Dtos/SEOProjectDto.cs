namespace SeoManagement.API.Models.Dtos
{
	public class SEOProjectDto
	{
		public int ProjectID { get; set; }
		public int UserID { get; set; }
		public string ProjectName { get; set; }
		public string Description { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public string Status { get; set; }
	}
}
