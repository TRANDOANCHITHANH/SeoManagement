namespace SeoManagement.Core.Entities.Dtos
{
	public class DailyStatDto
	{
		public DateTime AccessDate { get; set; }
		public int PageViews { get; set; }
		public int DayOfMonth { get; set; }
	}
}
