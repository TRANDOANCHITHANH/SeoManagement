namespace SeoManagement.Core.Entities.Dtos
{
	public class AccessStatsDto
	{
		public int TotalPageViews { get; set; }
		public int Today { get; set; }
		public int Yesterday { get; set; }
		public int ThisWeek { get; set; }
		public int LastWeek { get; set; }
		public int ThisMonth { get; set; }
		public int LastMonth { get; set; }
		public List<DailyStatDto> DailyStats { get; set; }

	}
}
