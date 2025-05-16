using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordRepository : IRepository<Keyword>
	{
		Task<List<Keyword>> GetByProjectIdAsync(int projectId);
		Task<Keyword> GetByKeywordAndDomainAsync(int projectId, string keyword, string domain);
	}
}
