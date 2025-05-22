namespace SeoManagement.Web.Models.ViewModels
{
	public class PerformanceDashboardViewModel
	{
		public int ProjectId { get; set; }
		public string ProjectName { get; set; }
		public string ProjectType { get; set; }
		public DateTime StartDate { get; set; }
		public List<PerformanceEntry> History { get; set; }
	}
	public class PerformanceEntry
	{
		public DateTime RecordedAt { get; set; }
		public double? AverageKeywordRank { get; set; }
		public double? AverageOnPageScore { get; set; }
		public double? PageSpeedScore { get; set; }
		public int? BacklinkCount { get; set; }
		public int? IndexedPageCount { get; set; }
		public int? UnindexedPageCount { get; set; }
	}
}
