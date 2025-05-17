using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOProjectService
	{
		Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, int? userId = null);
		Task<SEOProject> GetByIdAsync(int projectId);
		Task CreateSEOProjectAsync(SEOProject project);
		Task UpdateSEOProjectAsync(SEOProject project);
		Task DeleteSEOProjectAsync(int projectId);
		Task<List<SEOProject>> GetAllAsync();
	}
}
