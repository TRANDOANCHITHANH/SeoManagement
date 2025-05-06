using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IIndexCheckerUrlService
	{
		Task AddAsync(IndexCheckerUrl url);
		Task<List<IndexCheckerUrl>> GetByProjectIdAsync(int projectId);
		Task DeleteAsync(int id);
	}
}
