using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordRepository
	{
		Task<(IEnumerable<Keyword>, int)> GetPagedByProjectIdAsync(int projectId, int pageNumber, int pageSize);
		Task<Keyword> GetByIdAsync(int id);
		Task AddAsync(Keyword keyword);
		Task UpdateAsync(Keyword keyword);
		Task DeleteAsync(int id);
		Task AddKeywordHistoryAsync(KeywordHistory history);
	}
}
