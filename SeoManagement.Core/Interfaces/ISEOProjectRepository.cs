using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOProjectRepository
	{
		Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize);
		Task<SEOProject> GetByIdAsync(int projectId);
		Task AddAsync(SEOProject project);
		Task UpdateAsync(SEOProject project);
		Task DeleteAsync(int projectId);
	}
}
