namespace SeoManagement.API.Models.Dtos
{
	public class ContentDto
	{
		public int ContentID { get; set; }
		public int ProjectID { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public string MainKeyword { get; set; }
		public int? WordCount { get; set; }
		public string IndexStatus { get; set; }
		public bool CannibalizationFlag { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime LastModified { get; set; }
	}
}
