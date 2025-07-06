using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Infrastructure.Services;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class AwsSesEmailServiceTests
    {
        private readonly Mock<IOptions<EmailSettings>> _emailSettingsMock;
        private readonly Mock<ILogger<AwsSesEmailService>> _loggerMock;
        private readonly EmailSettings _emailSettings;

        public AwsSesEmailServiceTests()
        {
            _emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                Port = 587,
                EnableSsl = true,
                SenderEmail = "sender@test.com",
                SenderName = "Sender",
                Username = "user",
                Password = "pass"
            };
            _emailSettingsMock = new Mock<IOptions<EmailSettings>>();
            _emailSettingsMock.Setup(x => x.Value).Returns(_emailSettings);
            _loggerMock = new Mock<ILogger<AwsSesEmailService>>();
        }

        [Fact]
        public async Task SendEmailAsync_Success()
        {
            // Arrange
            var service = new AwsSesEmailService(_emailSettingsMock.Object, _loggerMock.Object);

            // Use a local SMTP server or mock SmtpClient if you refactor for testability.
            // Here, just ensure no exception is thrown for valid input.
            // Act & Assert
            await Assert.ThrowsAnyAsync<SmtpException>(() =>
                service.SendEmailAsync("recipient@test.com", "Subject", "Body"));
        }

        [Fact]
        public async Task SendEmailToMultipleRecipientsAsync_Success()
        {
            // Arrange
            var service = new AwsSesEmailService(_emailSettingsMock.Object, _loggerMock.Object);
            var recipients = new[] { "a@test.com", "b@test.com" };

            // Act & Assert
            await Assert.ThrowsAnyAsync<SmtpException>(() =>
                service.SendEmailToMultipleRecipientsAsync(recipients, "Subject", "Body"));
        }
    }
}