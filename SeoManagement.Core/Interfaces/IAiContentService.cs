using SeoManagement.Core.Entities.Dtos;

namespace SeoManagement.Core.Interfaces
{
	public interface IAiContentService
	{
		Task<string> GenerateAiContentSuggestion(string intent, string query, string existingContent, List<IntentScore> subIntents);
	}
}
