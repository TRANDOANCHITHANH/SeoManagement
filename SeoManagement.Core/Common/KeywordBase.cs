using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SeoManagement.Core.Common
{
	public class KeywordBase
	{
		public int? SearchVolume { get; set; }

		public int? Difficulty { get; set; }

		public decimal? CPC { get; set; }

		public string CompetitionValue { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		[Column(TypeName = "nvarchar(max)")]
		public string MonthlySearchVolumesJson { get; set; } = "[]";

		[NotMapped]
		public List<MonthlyVolume> MonthlySearchVolumes
		{
			get => JsonSerializer.Deserialize<List<MonthlyVolume>>(MonthlySearchVolumesJson ?? "[]") ?? new List<MonthlyVolume>();
			set => MonthlySearchVolumesJson = JsonSerializer.Serialize(value ?? new List<MonthlyVolume>());
		}
	}
	public class MonthlyVolume
	{
		[Required]
		public string Month { get; set; }

		[Required]
		public int Year { get; set; }

		[Required]
		public int Searches { get; set; }
	}
}
