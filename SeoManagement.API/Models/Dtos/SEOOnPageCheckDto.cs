namespace SeoManagement.API.Models.Dtos
{
	public class SEOOnPageCheckDto
	{
		public int CheckID { get; set; }
		public int ProjectID { get; set; }
		public string Url { get; set; }
		public string Title { get; set; }
		public string MetaDescription { get; set; }
		public string MainKeyword { get; set; }
		public int WordCount { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class SEOOnPageAnalysisResult
	{
		public bool IsTitleLengthOptimal { get; set; }
		public bool IsMetaDescriptionLengthOptimal { get; set; }
		public bool IsMainKeywordInTitle { get; set; }
		public bool IsMainKeywordInMetaDescription { get; set; }
		public bool IsWordCountSufficient { get; set; }

		public int HeadingCount { get; set; }
		public int ImageCountWithoutAlt { get; set; }
		public double KeywordDensity { get; set; }
		public int InternalLinkCount { get; set; }
		public int PageSpeedScore { get; set; }
		public string Summary { get; set; }
	}
}
