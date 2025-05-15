using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IBacklinkResultService
	{
		Task AddAsync(Backlink result);
		Task UpdateAsync(Backlink result);
		Task<List<Backlink>> GetByProjectIdAsync(int projectId);
		Task DeleteAsync(int id);
	}
}
