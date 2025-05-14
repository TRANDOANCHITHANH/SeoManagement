using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IIndexCheckerUrlRepository : IRepository<IndexCheckerUrl>
	{
		Task<List<IndexCheckerUrl>> GetByProjectIdAsync(int projectId);
	}
}
