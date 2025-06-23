using SeoManagement.Core.Entities.Dtos;

namespace SeoManagement.Web.Models.ViewModels
{
	public class IntentViewModel
	{
		public string Query { get; set; }
		public string Content { get; set; } = string.Empty;
		public string SeoSuggestion { get; set; } = string.Empty;
		public string AiContentSuggestion { get; set; } = string.Empty;
		public bool IsAiContentGenerated { get; set; }
		public int ProjectId { get; set; }
		public List<IntentScore> MainIntents { get; set; } = new List<IntentScore>();
		public List<IntentScore> SubIntents { get; set; } = new List<IntentScore>();
	}
}
