using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Email service implementation using standard SMTP
    /// </summary>
    public class SimpleSmtpEmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<SimpleSmtpEmailService> _logger;

        public SimpleSmtpEmailService(IOptions<EmailSettings> emailSettings, ILogger<SimpleSmtpEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {Recipient} using SMTP server {Server}:{Port}", 
                    recipient, _emailSettings.SmtpServer, _emailSettings.Port);
                
                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = _emailSettings.EnableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = 30000; // 30 seconds timeout
                    
                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;
                        message.To.Add(recipient);
                        
                        _logger.LogInformation("Sending email with subject: {Subject}", subject);
                        await client.SendMailAsync(message);
                        _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}. Error: {Error}", recipient, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SendEmailToMultipleRecipientsAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true)
        {
            try
            {
                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = _emailSettings.EnableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    
                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;
                        
                        foreach (var recipient in recipients)
                        {
                            message.To.Add(recipient);
                        }
                        
                        await client.SendMailAsync(message);
                        _logger.LogInformation("Email sent successfully to multiple recipients");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients. Error: {Error}", ex.Message);
                throw;
            }
        }
    }
} 