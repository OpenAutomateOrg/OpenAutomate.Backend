using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class EmailVerificationControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<EmailVerificationController>> _mockLogger;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly EmailVerificationController _controller;
        private readonly string _frontendUrl = "https://frontend.test";

        public EmailVerificationControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<EmailVerificationController>>();
            _appSettings = Options.Create(new AppSettings { FrontendUrl = _frontendUrl });
            _controller = new EmailVerificationController(
                _mockUserService.Object,
                _mockTokenService.Object,
                _mockNotificationService.Object,
                _appSettings,
                _mockLogger.Object
            );
        }

        #region VerifyEmail

        [Fact]
        public async Task VerifyEmail_InvalidToken_RedirectsWithInvalidTokenReason()
        {
            // Arrange
            _mockTokenService.Setup(s => s.ValidateEmailVerificationTokenAsync("badtoken")).ReturnsAsync((Guid?)null);

            // Act
            var result = await _controller.VerifyEmail("badtoken");

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("success=false", redirect.Url);
            Assert.Contains("reason=invalid-token", redirect.Url);
        }

        [Fact]
        public async Task VerifyEmail_VerificationFailed_RedirectsWithVerificationFailedReason()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockTokenService.Setup(s => s.ValidateEmailVerificationTokenAsync("token")).ReturnsAsync(userId);
            _mockUserService.Setup(s => s.VerifyUserEmailAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _controller.VerifyEmail("token");

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("success=false", redirect.Url);
            Assert.Contains("reason=verification-failed", redirect.Url);
        }

        [Fact]
        public async Task VerifyEmail_Success_RedirectsWithSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserResponse { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" };
            _mockTokenService.Setup(s => s.ValidateEmailVerificationTokenAsync("token")).ReturnsAsync(userId);
            _mockUserService.Setup(s => s.VerifyUserEmailAsync(userId)).ReturnsAsync(true);
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockNotificationService.Setup(s => s.SendWelcomeEmailAsync(user.Email, It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.VerifyEmail("token");

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("success=true", redirect.Url);
        }

        [Fact]
        public async Task VerifyEmail_Exception_RedirectsWithServerError()
        {
            // Arrange
            _mockTokenService.Setup(s => s.ValidateEmailVerificationTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.VerifyEmail("token");

            // Assert
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("success=false", redirect.Url);
            Assert.Contains("reason=server-error", redirect.Url);
        }

        #endregion

        #region ResendVerification

        [Fact]
        public async Task ResendVerification_UserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("User not authenticated", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task ResendVerification_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((UserResponse)null);

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("User not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task ResendVerification_EmailAlreadyVerified_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserResponse { Id = userId, Email = "test@example.com", IsEmailVerified = true };
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("already verified", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ResendVerification_SendVerificationFails_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserResponse { Id = userId, Email = "test@example.com", IsEmailVerified = false };
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.SendVerificationEmailAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Failed to send verification email", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ResendVerification_Success_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserResponse { Id = userId, Email = "test@example.com", IsEmailVerified = false };
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.SendVerificationEmailAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Verification email sent", ok.Value.ToString());
        }

        [Fact]
        public async Task ResendVerification_Exception_ReturnsServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserResponse { Id = userId, Email = "test@example.com", IsEmailVerified = false };
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.SendVerificationEmailAsync(userId)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.ResendVerification();

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("error", serverError.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}