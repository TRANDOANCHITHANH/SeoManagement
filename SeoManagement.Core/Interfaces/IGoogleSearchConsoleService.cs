namespace SeoManagement.Core.Interfaces
{
	public interface IGoogleSearchConsoleService
	{
		Task<List<(string Keyword, double Clicks, double Impressions, double Ctr, double Position)>> GetSeoDataAsync(string siteUrl, DateTime startDate, DateTime endDate);
	}
}
