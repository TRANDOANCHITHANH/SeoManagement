using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOOnPageCheckRepository : IRepository<SEOOnPageCheck>
	{
		Task<(List<SEOOnPageCheck> Items, int TotalItems)> GetPagedAsync(int projectId, int pageNumber, int pageSize);
	}
}
