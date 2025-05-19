namespace SeoManagement.API.Models.Dtos
{
	public class UserDto
	{
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public string FullName { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsActive { get; set; }
		public string Role { get; set; }

		public List<UserActionLimitDto> ActionLimits { get; set; } = new List<UserActionLimitDto>();
	}
}
