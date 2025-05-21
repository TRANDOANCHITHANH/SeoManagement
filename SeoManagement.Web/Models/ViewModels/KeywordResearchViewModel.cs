namespace SeoManagement.Web.Models.ViewModels
{
	public class KeywordResearchViewModel
	{
		public int ProjectId { get; set; }
		public string SeedKeyword { get; set; }
		public string Message { get; set; }
		public List<KeywordViewModel> Keywords { get; set; } = new List<KeywordViewModel>();

		public class KeywordViewModel
		{
			public string SeedKeyword { get; set; }
			public string SuggestedKeyword { get; set; }
			public int? SearchVolume { get; set; }
			public int? Difficulty { get; set; }
			public decimal? CPC { get; set; }
			public string CompetitionValue { get; set; }
			public string CreatedDate { get; set; }
			public List<MonthlyVolumeViewModel> MonthlySearchVolumes { get; set; } = new List<MonthlyVolumeViewModel>();
			public bool IsMainKeyword { get; set; }
		}

		public class MonthlyVolumeViewModel
		{
			public string Month { get; set; }
			public int Year { get; set; }
			public int Searches { get; set; }
		}
	}
}