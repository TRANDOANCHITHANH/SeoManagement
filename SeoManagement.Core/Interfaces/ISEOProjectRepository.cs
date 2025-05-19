using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface ISEOProjectRepository : IRepository<SEOProject>
	{
		Task<(List<SEOProject> Items, int TotalItems)> GetPagedAsync(int pageNumber, int pageSize, int? userId = null);
		Task<IEnumerable<SEOProject>> GetAllAsync(string projectType);
	}
}
