using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordSuggestionRepository : IRepository<SeedKeyword>
	{
		Task<List<SeedKeyword>> GetByProjectIdAsync(int projectId);
		Task<List<SeedKeyword>> GetSuggestionsAsync(int projectId, string seedKeyword);
		Task AddSuggestionsAsync(List<SeedKeyword> suggestions);
	}
}
