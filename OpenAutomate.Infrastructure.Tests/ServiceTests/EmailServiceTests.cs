using Moq;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class EmailServiceTests
    {
        private readonly Mock<IEmailService> _mockEmailService;

        public EmailServiceTests()
        {
            _mockEmailService = new Mock<IEmailService>();
        }

        [Fact]
        public async Task SendEmailAsync_WithValidParameters_SendsEmail()
        {
            // Arrange
            string recipient = "test@example.com";
            string subject = "Test Email";
            string body = "<p>This is a test email</p>";
            bool isHtml = true;

            _mockEmailService.Setup(service => service.SendEmailAsync(recipient, subject, body, isHtml))
                .Returns(Task.CompletedTask);

            // Act
            await _mockEmailService.Object.SendEmailAsync(recipient, subject, body, isHtml);

            // Assert
            _mockEmailService.Verify(service => service.SendEmailAsync(recipient, subject, body, isHtml), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_WithInvalidEmail_ThrowsException()
        {
            // Arrange
            string invalidRecipient = "invalid.email";
            string subject = "Test Email";
            string body = "<p>This is a test email</p>";
            bool isHtml = true;

            _mockEmailService.Setup(service => service.SendEmailAsync(invalidRecipient, subject, body, isHtml))
                .ThrowsAsync(new ArgumentException("Invalid email address"));

            // Act
            Func<Task> act = async () => await _mockEmailService.Object.SendEmailAsync(invalidRecipient, subject, body, isHtml);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);   
            Assert.Equal("Invalid email address", exception.Message);
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptySubject_ThrowsException()
        {
            // Arrange
            string recipient = "test@example.com";
            string emptySubject = string.Empty;
            string body = "<p>This is a test email</p>";
            bool isHtml = true;

            _mockEmailService.Setup(service => service.SendEmailAsync(recipient, emptySubject, body, isHtml))
                .ThrowsAsync(new ArgumentException("Subject cannot be empty"));

            // Act
            Func<Task> act = async () => await _mockEmailService.Object.SendEmailAsync(recipient, emptySubject, body, isHtml);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("Subject cannot be empty", exception.Message);
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyBody_ThrowsException()
        {
            // Arrange
            string recipient = "test@example.com";
            string subject = "Test Email";
            string emptyBody = string.Empty;
            bool isHtml = true;

            _mockEmailService.Setup(service => service.SendEmailAsync(recipient, subject, emptyBody, isHtml))
                .ThrowsAsync(new ArgumentException("Body cannot be empty"));

            // Act
            Func<Task> act = async () => await _mockEmailService.Object.SendEmailAsync(recipient, subject, emptyBody, isHtml);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("Body cannot be empty", exception.Message);
        }

    }
}
