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
	}
}
