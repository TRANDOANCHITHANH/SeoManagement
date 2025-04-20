using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISiteService
	{
		Task<List<Site>> GetSitesByUserIdAsync(int userId);
		Task<Site> GetSiteByIdAsync(int siteId, int userId);
		Task AddSiteAsync(Site site, int userId);
	}
}
