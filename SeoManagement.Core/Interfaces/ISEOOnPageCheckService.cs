using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOOnPageCheckService
	{
		Task<(List<SEOOnPageCheck> Items, int TotalItems)> GetPagedAsync(int projectId, int pageNumber, int pageSize);
		Task<SEOOnPageCheck> GetByIdAsync(int id);
		Task CreateSEOOnPageCheckAsync(SEOOnPageCheck check);
		Task UpdateSEOOnPageCheckAsync(SEOOnPageCheck check);
		Task DeleteSEOOnPageCheckAsync(int id);
	}
}
