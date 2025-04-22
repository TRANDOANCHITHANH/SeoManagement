using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOOnPageCheckRepository
	{
		Task<(List<SEOOnPageCheck> Items, int TotalItems)> GetPagedAsync(int projectId, int pageNumber, int pageSize);
		Task<SEOOnPageCheck> GetByIdAsync(int id);
		Task AddSEOOnPageCheckAsync(SEOOnPageCheck check);
		Task UpdateSEOOnPageCheckAsync(SEOOnPageCheck check);
		Task DeleteSEOOnPageCheckAsync(int id);
	}
}
