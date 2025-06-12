using System.Text.Json.Serialization;

namespace SeoManagement.Core.Entities.Dtos
{
	public class IntentScore
	{
		[JsonPropertyName("intent")]
		public string Intent { get; set; }

		[JsonPropertyName("score")]
		public float Score { get; set; }
	}
}
