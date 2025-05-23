using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeoManagement.Web.Models.ViewModels
{
	public class ContentViewmodel
	{
		public int ProjectId { get; set; }
		[BindNever]
		public string? ProjectName { get; set; }

		public string? TargetKeyword { get; set; }

		public string? Content { get; set; }
		[BindNever]
		public string? Message { get; set; }
		[BindNever]
		public ContentResultViewModel? Result { get; set; }

		public class ContentResultViewModel
		{
			public string KeywordUsage { get; set; }
			public string KeywordDensity { get; set; }
			public string RelatedKeywords { get; set; }
			public string AltAttributeIssues { get; set; }
			public string ImageSuggestion { get; set; }
			public string TitleIssues { get; set; }
			public string MetaSuggestions { get; set; }
			public string WordCount { get; set; }
			public string ReadabilityScore { get; set; }
			public string ToneOfVoice { get; set; }
			public string OriginalityCheck { get; set; }
			public string ContentStructureIssues { get; set; }
			public string LinkIssues { get; set; }
		}
	}
}
