using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/test-invitation")]
    public class TestInvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<TestInvitationController> _logger;

        public TestInvitationController(
            IInvitationService invitationService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<TestInvitationController> logger)
        {
            _invitationService = invitationService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Test gửi email mời tham gia tổ chức
        /// </summary>
        /// <param name="email">Email của người được mời</param>
        /// <param name="name">Tên của người được mời</param>
        /// <returns>Kết quả thao tác</returns>
        [HttpGet("send-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTestInvitationEmail(
            [FromQuery] string email,
            [FromQuery] string name = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                _logger.LogInformation("Gửi email test đến {Email}", email);
                
                // Hiển thị thông tin cấu hình email
                var configInfo = new
                {
                    EmailService = _emailService.GetType().Name,
                    SmtpServer = _emailSettings.SmtpServer,
                    Port = _emailSettings.Port,
                    EnableSsl = _emailSettings.EnableSsl,
                    SenderEmail = _emailSettings.SenderEmail,
                    SenderName = _emailSettings.SenderName,
                    Username = _emailSettings.Username != null ? "Configured" : "Not configured",
                    Password = _emailSettings.Password != null ? "Configured" : "Not configured",
                    FrontendUrl = _configuration["FrontendUrl"]
                };
                
                _logger.LogInformation("Email configuration: {@Config}", configInfo);
                
                // Tìm user và organization đầu tiên làm mẫu
                var firstUser = await _unitOfWork.Users.GetFirstOrDefaultAsync();
                if (firstUser == null)
                {
                    _logger.LogWarning("Không tìm thấy user nào trong hệ thống để làm người mời");
                    return BadRequest(new { message = "Không tìm thấy user nào trong hệ thống để làm người mời" });
                }
                
                var firstOrg = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync();
                if (firstOrg == null)
                {
                    _logger.LogWarning("Không tìm thấy tổ chức nào trong hệ thống");
                    return BadRequest(new { message = "Không tìm thấy tổ chức nào trong hệ thống" });
                }
                
                _logger.LogInformation("Sử dụng user {UserId} và organization {OrgId} để gửi lời mời", 
                    firstUser.Id, firstOrg.Id);

                // Gửi lời mời
                await _invitationService.SendInvitationAsync(
                    firstUser.Id,
                    firstOrg.Id,
                    email,
                    name ?? email);

                return Ok(new { 
                    message = "Đã gửi email lời mời thành công",
                    user = new { Id = firstUser.Id, Email = firstUser.Email },
                    organization = new { Id = firstOrg.Id, Name = firstOrg.Name },
                    config = configInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email mời: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = $"Có lỗi xảy ra: {ex.Message}", 
                    details = ex.ToString(),
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        
        /// <summary>
        /// Test gửi email đơn giản
        /// </summary>
        /// <param name="email">Email người nhận</param>
        /// <returns>Kết quả gửi email</returns>
        [HttpGet("simple-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendSimpleEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                _logger.LogInformation("Gửi email đơn giản test đến {Email}", email);
                
                // Gửi email đơn giản không thông qua InvitationService
                string subject = "Test Email từ OpenAutomate";
                string content = $@"
                <html>
                <body>
                    <h2>Email Test</h2>
                    <p>Đây là email test từ OpenAutomate.</p>
                    <p>Thời gian gửi: {DateTime.Now}</p>
                </body>
                </html>";
                
                await _emailService.SendEmailAsync(email, subject, content, true);
                
                return Ok(new { message = "Đã gửi email đơn giản thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email đơn giản: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = $"Có lỗi xảy ra: {ex.Message}",
                    details = ex.ToString(),
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
} 