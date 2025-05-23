using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeoManagement.Core.Entities
{
	public class ContentOptimizationAnalysis
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectID { get; set; }

		[Required]
		public string TargetKeyword { get; set; }

		[Required]
		public string Content { get; set; }

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

		public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("ProjectID")]
		public virtual SEOProject Project { get; set; }
	}
}
