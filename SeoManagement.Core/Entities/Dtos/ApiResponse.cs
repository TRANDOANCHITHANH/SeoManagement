using System.Text.Json.Serialization;

namespace SeoManagement.Core.Entities.Dtos
{
	public class ApiResponse
	{
		[JsonPropertyName("keyword")]
		public string Keyword { get; set; }

		[JsonPropertyName("main_intents")]
		public List<IntentScore> MainIntents { get; set; }

		[JsonPropertyName("sub_intents")]
		public List<IntentScore> SubIntents { get; set; }
	}
}
