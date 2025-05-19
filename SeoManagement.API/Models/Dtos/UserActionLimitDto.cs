namespace SeoManagement.API.Models.Dtos
{
	public class UserActionLimitDto
	{
		public string ActionType { get; set; }
		public int DailyLimit { get; set; }
	}
}
