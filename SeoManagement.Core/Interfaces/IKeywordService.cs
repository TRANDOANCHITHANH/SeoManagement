using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordService
	{
		Task<(IEnumerable<Keyword>, int)> GetPagedAsync(int projectId, int pageNumber, int pageSize);
		Task<Keyword> GetByIdAsync(int id);
		Task CreateKeywordAsync(Keyword keyword);
		Task UpdateKeywordAsync(Keyword keyword);
		Task DeleteKeywordAsync(int id);
	}
}
