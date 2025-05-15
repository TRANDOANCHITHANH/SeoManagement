using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IBacklinkResultRepository : IRepository<Backlink>
	{
		Task<List<Backlink>> GetByProjectIdAsync(int projectId);
	}
}
