using SeoManagement.Core.Entities;

namespace SeoManagement.Core.Interfaces
{
	public interface IKeywordResearchService : IService<SeedKeyword>
	{
		Task<List<SeedKeyword>> ResearchKeywordsAsync(int projectId, string seedKeyword);
	}
}
