using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/test-email")]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<TestEmailController> _logger;

        public TestEmailController(
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<TestEmailController> logger)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Test gửi email đơn giản
        /// </summary>
        /// <param name="to">Email người nhận</param>
        /// <param name="subject">Tiêu đề email (mặc định: Test Email)</param>
        /// <returns>Kết quả gửi email</returns>
        [HttpGet("send")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTestEmail(
            [FromQuery] string to,
            [FromQuery] string subject = "Test Email")
        {
            try
            {
                if (string.IsNullOrEmpty(to))
                {
                    return BadRequest(new { message = "Email người nhận không được để trống" });
                }

                _logger.LogInformation("Sending test email to {Recipient}", to);
                
                // Hiển thị thông tin cấu hình email
                var configInfo = new
                {
                    SmtpServer = _emailSettings.SmtpServer,
                    Port = _emailSettings.Port,
                    EnableSsl = _emailSettings.EnableSsl,
                    SenderEmail = _emailSettings.SenderEmail,
                    SenderName = _emailSettings.SenderName,
                    Username = _emailSettings.Username != null ? "Configured" : "Not configured",
                    Password = _emailSettings.Password != null ? "Configured" : "Not configured"
                };
                
                _logger.LogInformation("Email configuration: {@Config}", configInfo);

                // Chuẩn bị nội dung email
                string emailContent = $@"
                <html>
                <body>
                    <h2>Test Email</h2>
                    <p>Đây là email test từ OpenAutomate.</p>
                    <p>Thời gian gửi: {DateTime.Now}</p>
                </body>
                </html>";

                // Gửi email
                await _emailService.SendEmailAsync(to, subject, emailContent, true);

                return Ok(new { 
                    message = "Email đã được gửi thành công", 
                    config = configInfo 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = $"Lỗi khi gửi email: {ex.Message}", 
                    details = ex.ToString() 
                });
            }
        }
    }
} 