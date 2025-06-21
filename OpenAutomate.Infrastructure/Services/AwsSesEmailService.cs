using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Email service implementation using AWS SES SMTP
    /// </summary>
    public class AwsSesEmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<AwsSesEmailService> _logger;

        public AwsSesEmailService(IOptions<EmailSettings> emailSettings, ILogger<AwsSesEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true)
        {
            try
            {
                using (var client = CreateSmtpClient())
                using (var message = CreateMailMessage(subject, body, isHtml))
                {
                    message.To.Add(recipient);
                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SendEmailToMultipleRecipientsAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true)
        {
            try
            {
                using (var client = CreateSmtpClient())
                using (var message = CreateMailMessage(subject, body, isHtml))
                {
                    foreach (var recipient in recipients)
                    {
                        message.To.Add(recipient);
                    }

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email sent successfully to multiple recipients");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients");
                throw;
            }
        }

        /// <summary>
        /// Creates and configures a new SMTP client
        /// </summary>
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!_emailSettings.EnableSsl)
            {
                _logger.LogWarning("SSL is disabled in configuration but was enforced for security. Please update your configuration to match actual usage.");
            }

            return client;
        }

        /// <summary>
        /// Creates a new mail message with the sender information
        /// </summary>
        private MailMessage CreateMailMessage(string subject, string body, bool isHtml)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            return message;
        }
    }
} 