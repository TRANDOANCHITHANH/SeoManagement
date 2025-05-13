using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordRepository : IRepository<Keyword>
	{
		Task<(IEnumerable<Keyword>, int)> GetPagedByProjectIdAsync(int projectId, int pageNumber, int pageSize);
		Task AddKeywordHistoryAsync(KeywordHistory history);
	}
}
