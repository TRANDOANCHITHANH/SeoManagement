using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordSuggestionRepository : IRepository<KeywordSuggestion>
	{
		Task<List<KeywordSuggestion>> GetByProjectIdAsync(int projectId);
		Task<List<KeywordSuggestion>> GetSuggestionsAsync(int projectId, string seedKeyword);
		Task AddSuggestionsAsync(List<KeywordSuggestion> suggestions);
	}
}
