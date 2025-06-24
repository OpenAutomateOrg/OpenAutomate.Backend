using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class EmailTestControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<EmailTestController>> _mockLogger;
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly EmailTestController _controller;

        public EmailTestControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<EmailTestController>>();

            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                Port = 25,
                EnableSsl = true,
                SenderEmail = "sender@test.com",
                SenderName = "Sender",
                Username = "user",
                Password = "pass"
            };
            _emailSettings = Options.Create(emailSettings);

            _controller = new EmailTestController(
                _mockEmailService.Object,
                _mockLogger.Object,
                _emailSettings
            );
        }

        #region GetEmailStatus Tests



        [Fact]
        public void GetEmailStatus_WithValidConfig_ReturnsOkAndConfigured()
        {
            var result = _controller.GetEmailStatus();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Use reflection to check for property existence and value
            var type = okResult.Value.GetType();
            var isConfiguredProp = type.GetProperty("IsConfigured");
            Assert.NotNull(isConfiguredProp);
            var isConfigured = isConfiguredProp.GetValue(okResult.Value);
            Assert.True(isConfigured is bool b && b);

            var smtpServer = type.GetProperty("SmtpServer")?.GetValue(okResult.Value);
            var port = type.GetProperty("Port")?.GetValue(okResult.Value);
            var senderEmail = type.GetProperty("SenderEmail")?.GetValue(okResult.Value);
            var senderName = type.GetProperty("SenderName")?.GetValue(okResult.Value);
            var enableSsl = type.GetProperty("EnableSsl")?.GetValue(okResult.Value);
            var hasCredentials = type.GetProperty("HasCredentials")?.GetValue(okResult.Value);

            Assert.Equal("smtp.test.com", smtpServer);
            Assert.Equal(25, port);
            Assert.Equal("sender@test.com", senderEmail);
            Assert.Equal("Sender", senderName);
            Assert.True(enableSsl is bool b2 && b2);
            Assert.True(hasCredentials is bool b3 && b3);
        }


        [Fact]
        public void GetEmailStatus_WithIncompleteConfig_ReturnsNotConfigured()
        {
            var incompleteSettings = new EmailSettings
            {
                SmtpServer = "",
                Port = 0,
                SenderEmail = ""
            };
            var controller = new EmailTestController(
                _mockEmailService.Object,
                _mockLogger.Object,
                Options.Create(incompleteSettings)
            );

            var result = controller.GetEmailStatus();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Use reflection to check for property existence and value
            var type = okResult.Value.GetType();
            var isConfiguredProp = type.GetProperty("IsConfigured");
            Assert.NotNull(isConfiguredProp);
            var isConfigured = isConfiguredProp.GetValue(okResult.Value);
            Assert.False(isConfigured is bool b && b);
        }


        // ... (các test khác)

        [Fact]
        public async Task SendWelcomeEmailGet_WithValidEmail_ReturnsOk()
        {
            var email = "test@test.com";
            _mockEmailService
                .Setup(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true))
                .Returns(Task.CompletedTask);

            var result = await _controller.SendWelcomeEmailGet(email);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains(email, okResult.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Once
            );
        }

        [Fact]
        public async Task SendWelcomeEmailGet_WithEmptyEmail_ReturnsBadRequest()
        {
            var result = await _controller.SendWelcomeEmailGet("");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Email address is required", badRequest.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Never
            );
        }

        [Fact]
        public async Task SendWelcomeEmailGet_WithNullEmail_ReturnsBadRequest()
        {
            var result = await _controller.SendWelcomeEmailGet((string?)null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Email address is required", badRequest.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Never
            );
        }

        [Fact]
        public async Task SendWelcomeEmailGet_WhenServiceThrows_ReturnsInternalServerError()
        {
            var email = "test@test.com";
            _mockEmailService
                .Setup(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true))
                .ThrowsAsync(new Exception("SMTP error"));

            var result = await _controller.SendWelcomeEmailGet(email);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.NotNull(statusResult.Value);
            Assert.Contains("An error occurred", statusResult.Value!.ToString()!);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        // ... (các test cho POST cũng sửa tương tự)
        [Fact]
        public async Task SendWelcomeEmailPost_WithValidEmail_ReturnsOk()
        {
            var email = "test@test.com";
            _mockEmailService
                .Setup(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true))
                .Returns(Task.CompletedTask);

            var result = await _controller.SendWelcomeEmailPost(email);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains(email, okResult.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Once
            );
        }

        [Fact]
        public async Task SendWelcomeEmailPost_WithEmptyEmail_ReturnsBadRequest()
        {
            var result = await _controller.SendWelcomeEmailPost("");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Email address is required", badRequest.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Never
            );
        }

        [Fact]
        public async Task SendWelcomeEmailPost_WithNullEmail_ReturnsBadRequest()
        {
            var result = await _controller.SendWelcomeEmailPost((string?)null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Email address is required", badRequest.Value!.ToString()!);
            _mockEmailService.Verify(
                s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true),
                Times.Never
            );
        }

        [Fact]
        public async Task SendWelcomeEmailPost_WhenServiceThrows_ReturnsInternalServerError()
        {
            var email = "test@test.com";
            _mockEmailService
                .Setup(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), true))
                .ThrowsAsync(new Exception("SMTP error"));

            var result = await _controller.SendWelcomeEmailPost(email);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.NotNull(statusResult.Value);
            Assert.Contains("An error occurred", statusResult.Value!.ToString()!);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }


        #endregion
    }
}
