using SeoManagement.Core.Enum;

namespace SeoManagement.Core.Entities.Dtos
{
	public class Alert
	{
		public AlertType Type { get; set; }
		public string Message { get; set; }
		public int EntityId { get; set; }
		public int ProjectId { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
