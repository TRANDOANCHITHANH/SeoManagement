using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IWebsiteInsightRepository : IRepository<WebsiteInsight>
	{
		Task<List<WebsiteInsight>> GetByProjectIdAsync(int projectId);
	}
}
