using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IPageSpeedResultService
	{
		Task AddAsync(PageSpeedResult pageSpeedResult);
		Task UpdateAsync(PageSpeedResult pageSpeedResult);
		Task<List<PageSpeedResult>> GetByProjectIdAsync(int projectId);
		Task DeleteAsync(int id);
	}
}
