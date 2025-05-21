using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordResearchService : IService<KeywordSuggestion>
	{
		Task<List<KeywordSuggestion>> ResearchKeywordsAsync(int projectId, string seedKeyword);
	}
}
