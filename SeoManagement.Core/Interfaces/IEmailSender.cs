namespace SeoManagement.Core.Interfaces
{
	public interface IEmailSender
	{
		Task SendEmailAsync(string email, string subject, string htmlMessage);
		Task SendEmailDailyAsync(string toEmail, string subject, string htmlBody, MemoryStream attachmentStream = null, string attachmentFileName = null);
		public MemoryStream GenerateExcelReport(dynamic results, int projectId);
		public string GenerateHtmlReport(dynamic results, string projectName);
	}
}
