namespace SeoManagement.Web.Models.ViewModels
{
	public class ResetPasswordViewModel
	{
		public string UserId { get; set; }
		public string Token { get; set; }
		public string Password { get; set; }
		public string ConfirmPassword { get; set; }
	}
}
