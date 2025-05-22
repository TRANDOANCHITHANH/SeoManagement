using System.ComponentModel.DataAnnotations;

namespace SeoManagement.Core.Entities
{
	public class SEOPerformanceHistory
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProjectId { get; set; }

		public SEOProject Project { get; set; }

		[Required]
		public string ProjectType { get; set; }

		public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

		public double? AverageKeywordRank { get; set; }
		public double? AverageOnPageScore { get; set; }
		public double? PageSpeedScore { get; set; }
		public int? BacklinkCount { get; set; }
		public int? IndexedPageCount { get; set; }
		public int? UnindexedPageCount { get; set; }
	}
}
