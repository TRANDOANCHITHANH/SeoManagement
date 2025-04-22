using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Web.Models.ViewModels
{
	public class SEOOnPageCheckViewModel
	{
		public int CheckID { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required(ErrorMessage = "URL là bắt buộc")]
		[StringLength(500)]
		public string Url { get; set; }

		[StringLength(200)]
		public string Title { get; set; }

		[StringLength(500)]
		public string MetaDescription { get; set; }

		[StringLength(100)]
		public string MainKeyword { get; set; }

		public int WordCount { get; set; }

		public DateTime CreatedAt { get; set; }
	}

	public class SEOOnPageAnalysisResultViewModel
	{
		public bool IsTitleLengthOptimal { get; set; }
		public bool IsMetaDescriptionLengthOptimal { get; set; }
		public bool IsMainKeywordInTitle { get; set; }
		public bool IsMainKeywordInMetaDescription { get; set; }
		public bool IsWordCountSufficient { get; set; }
		public string Summary { get; set; }
	}
}
