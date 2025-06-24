using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only admins can test email functionality
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailTestController(
            IEmailService emailService, 
            ILogger<EmailTestController> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Gets the email configuration status
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetEmailStatus()
        {
            // Check if the email settings are configured
            bool isSmtpConfigured = !string.IsNullOrEmpty(_emailSettings.SmtpServer) && 
                                   _emailSettings.Port > 0 &&
                                   !string.IsNullOrEmpty(_emailSettings.Username) &&
                                   !string.IsNullOrEmpty(_emailSettings.Password) &&
                                   !string.IsNullOrEmpty(_emailSettings.SenderEmail);

            return Ok(new
            {
                IsConfigured = isSmtpConfigured,
                SmtpServer = _emailSettings.SmtpServer,
                Port = _emailSettings.Port,
                SenderEmail = _emailSettings.SenderEmail,
                SenderName = _emailSettings.SenderName,
                EnableSsl = _emailSettings.EnableSsl,
                // Don't expose the username and password
                HasCredentials = !string.IsNullOrEmpty(_emailSettings.Username) && 
                                !string.IsNullOrEmpty(_emailSettings.Password)
            });
        }

        /// <summary>
        /// Sends a test welcome email to the specified recipient (GET method)
        /// </summary>
        /// <param name="email">Email address of the recipient</param>
        /// <returns>Result of the operation</returns>
        [HttpGet("send-welcome")]
        public async Task<IActionResult> SendWelcomeEmailGet([FromQuery] string email)
        {
            return await SendWelcomeEmailInternal(email);
        }

        /// <summary>
        /// Sends a test welcome email to the specified recipient (POST method)
        /// </summary>
        /// <param name="email">Email address of the recipient</param>
        /// <returns>Result of the operation</returns>
        [HttpPost("send-welcome")]
        public async Task<IActionResult> SendWelcomeEmailPost([FromQuery] string email)
        {
            return await SendWelcomeEmailInternal(email);
        }

        /// <summary>
        /// Internal method to send welcome email
        /// </summary>
        private async Task<IActionResult> SendWelcomeEmailInternal(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email address is required");
            }

            try
            {
                // Create a simple welcome email with HTML
                var subject = "Welcome to OpenAutomate!";
                var body = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                        .header { background-color: #0078d4; color: white; padding: 10px 20px; text-align: center; }
                        .content { padding: 20px; border: 1px solid #ddd; border-top: none; }
                        .button { display: inline-block; background-color: #0078d4; color: white; text-decoration: none; padding: 10px 20px; border-radius: 4px; }
                        .footer { margin-top: 20px; font-size: 12px; color: #777; text-align: center; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to OpenAutomate</h2>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>Thank you for joining OpenAutomate! We're excited to have you on board.</p>
                            <p>With OpenAutomate, you can automate processes, manage bots, and so much more.</p>
                            <p>
                                <a href='https://open-bot.live' class='button'>Get Started</a>
                            </p>
                            <p>If you have any questions, please don't hesitate to contact our support team.</p>
                            <p>Best regards,<br>The OpenAutomate Team</p>
                        </div>
                        <div class='footer'>
                            <p>© 2023 OpenAutomate. All rights reserved.</p>
                            <p>This is a system-generated email. Please do not reply to this message.</p>
                        </div>
                    </div>
                </body>
                </html>";

                // Send the email
                await _emailService.SendEmailAsync(email, subject, body);
                
                _logger.LogInformation("Welcome email sent to {Email}", email);
                return Ok($"Welcome email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", email);
                return StatusCode(500, "An error occurred while sending the email. Please try again later.");
            }
        }
    }
} 