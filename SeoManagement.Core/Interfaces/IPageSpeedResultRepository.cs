using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IPageSpeedResultRepository : IRepository<PageSpeedResult>
	{
		Task<List<PageSpeedResult>> GetByProjectIdAsync(int projectId);
	}
}
