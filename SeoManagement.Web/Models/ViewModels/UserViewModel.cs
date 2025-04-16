namespace SeoManagement.Web.Models.ViewModels
{
	public class UserViewModel
	{
		public int UserID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string FullName { get; set; }
		public string Role { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsActive { get; set; }
	}
}
