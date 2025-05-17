using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeoManagement.Core.Interfaces;
using System.Net;
using System.Net.Mail;

namespace SeoManagement.Infrastructure.Services
{
	public class EmailSender : IEmailSender
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<EmailSender> _logger;
		public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			_logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, subject);
			_logger.LogInformation("SMTP Host: {Host}, Port: {Port}", _configuration["Smtp:Host"], _configuration["Smtp:Port"]);
			_logger.LogInformation("From Email: {FromEmail}, Username: {Username}", _configuration["Smtp:FromEmail"], _configuration["Smtp:Username"]);
			var smtpClient = new SmtpClient(_configuration["Smtp:Host"])
			{
				Port = int.Parse(_configuration["Smtp:Port"]),
				Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]),
				EnableSsl = true,
			};

			var mailMessage = new MailMessage
			{
				From = new MailAddress(_configuration["Smtp:FromEmail"]),
				Subject = subject,
				Body = htmlMessage,
				IsBodyHtml = true,
			};
			mailMessage.To.Add(email);

			await smtpClient.SendMailAsync(mailMessage);
		}

		public async Task SendEmailDailyAsync(string toEmail, string subject, string htmlBody, MemoryStream attachmentStream = null, string attachmentFileName = null)
		{
			var smtpServer = _configuration["Smtp:Host"];
			var smtpPort = int.Parse(_configuration["Smtp:Port"]);
			var senderEmail = _configuration["Smtp:FromEmail"];
			var senderPassword = _configuration["Smtp:Password"];
			var senderName = _configuration["Smtp:Username"];

			using var client = new SmtpClient(smtpServer, smtpPort)
			{
				EnableSsl = true,
				Credentials = new NetworkCredential(senderEmail, senderPassword)
			};

			var mailMessage = new MailMessage
			{
				From = new MailAddress(senderEmail, senderName),
				Subject = subject,
				Body = htmlBody,
				IsBodyHtml = true
			};
			mailMessage.To.Add(toEmail);

			if (attachmentStream != null && !string.IsNullOrEmpty(attachmentFileName))
			{
				attachmentStream.Position = 0;
				var attachment = new Attachment(attachmentStream, attachmentFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
				mailMessage.Attachments.Add(attachment);
			}

			await client.SendMailAsync(mailMessage);
		}

		public MemoryStream GenerateExcelReport(dynamic results, int projectId)
		{
			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("KeywordRankReport");
			var currentRow = 1;

			// Tiêu đề cột
			worksheet.Cell(currentRow, 1).Value = "Từ khóa";
			worksheet.Cell(currentRow, 2).Value = "Domain";
			worksheet.Cell(currentRow, 3).Value = "Vị trí hiện tại";
			worksheet.Cell(currentRow, 4).Value = "Vị trí cũ";
			worksheet.Cell(currentRow, 5).Value = "Vị trí cao nhất";
			worksheet.Cell(currentRow, 6).Value = "Top Volume";
			worksheet.Cell(currentRow, 7).Value = "Ngày cập nhật cuối";
			worksheet.Range(currentRow, 1, currentRow, 7).Style.Font.Bold = true;

			currentRow++;
			foreach (var result in results)
			{
				worksheet.Cell(currentRow, 1).Value = result.Keyword;
				worksheet.Cell(currentRow, 2).Value = result.Domain;
				worksheet.Cell(currentRow, 3).Value = result.CurrentPosition > 0 ? result.CurrentPosition.ToString() : "N/A";
				worksheet.Cell(currentRow, 4).Value = result.PreviousPosition > 0 ? result.PreviousPosition.ToString() : "N/A";
				worksheet.Cell(currentRow, 5).Value = result.BestPosition > 0 ? result.BestPosition.ToString() : "N/A";
				worksheet.Cell(currentRow, 6).Value = result.TopVolume > 0 ? result.TopVolume.ToString() : "N/A";
				worksheet.Cell(currentRow, 7).Value = result.LastUpdate.ToString("dd/MM/yyyy HH:mm:ss");

				currentRow++;
			}

			worksheet.Columns().AdjustToContents();

			var stream = new MemoryStream();
			workbook.SaveAs(stream);
			stream.Position = 0;
			return stream;
		}

		public string GenerateHtmlReport(dynamic results, string projectName)
		{
			var html = new System.Text.StringBuilder();
			html.AppendLine("<html><body>");
			html.AppendLine($"<h2>Báo cáo thứ hạng từ khóa - Dự án: {projectName}</h2>");
			html.AppendLine($"<p>Ngày gửi: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
			html.AppendLine("<table border='1' style='border-collapse: collapse;'>");
			html.AppendLine("<tr style='font-weight: bold; background-color: #f2f2f2;'>");
			html.AppendLine("<th>Từ khóa</th><th>Domain</th><th>Vị trí hiện tại</th><th>Vị trí cũ</th><th>Vị trí cao nhất</th><th>Top Volume</th><th>Ngày cập nhật cuối</th>");
			html.AppendLine("</tr>");

			foreach (var result in results)
			{
				html.AppendLine("<tr>");
				html.AppendLine($"<td>{result.Keyword}</td>");
				html.AppendLine($"<td>{result.Domain}</td>");
				html.AppendLine($"<td>{(result.CurrentPosition > 0 ? result.CurrentPosition.ToString() : "N/A")}</td>");
				html.AppendLine($"<td>{(result.PreviousPosition > 0 ? result.PreviousPosition.ToString() : "N/A")}</td>");
				html.AppendLine($"<td>{(result.BestPosition > 0 ? result.BestPosition.ToString() : "N/A")}</td>");
				html.AppendLine($"<td>{(result.TopVolume > 0 ? result.TopVolume.ToString() : "N/A")}</td>");
				html.AppendLine($"<td>{result.LastUpdate:dd/MM/yyyy HH:mm:ss}</td>");
				html.AppendLine("</tr>");
			}

			html.AppendLine("</table>");
			html.AppendLine("</body></html>");
			return html.ToString();
		}
	}
}
