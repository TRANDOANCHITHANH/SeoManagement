using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IIndexCheckerUrlRepository
	{
		Task AddAsync(IndexCheckerUrl url);
		Task<List<IndexCheckerUrl>> GetByProjectIdAsync(int projectId);
		Task DeleteAsync(int id);
	}
}
