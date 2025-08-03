using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class AuthServiceTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthService _authService;

        public AuthServiceTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tokenServiceMock = new Mock<ITokenService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _configurationMock = new Mock<IConfiguration>();
            _authService = new AuthService(
                _unitOfWorkMock.Object,
                _tokenServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task RegisterAsync_ShouldRegisterUser_WhenEmailDoesNotExist()
        {
            // Arrange
            var request = new RegistrationRequest
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };
            _unitOfWorkMock.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Users.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.RegisterAsync(request, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Email, result.Email);
            _unitOfWorkMock.Verify(u => u.Users.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var request = new RegistrationRequest
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };
            _unitOfWorkMock.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<UserAlreadyExistsException>(() => 
                _authService.RegisterAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                // Use empty password to bypass password verification
                // This simulates an external login (e.g., Google) where password verification is skipped
                Password = ""
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                IsEmailVerified = true
            };

            var authResponse = new AuthenticationResponse
            {
                Token = "jwt_token",
                RefreshToken = "refresh_token",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
            };

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateTokens(It.IsAny<User>(), It.IsAny<string>()))
                .Returns(authResponse);

            // Act
            var result = await _authService.AuthenticateAsync(request, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(authResponse.Token, result.Token);
            Assert.Equal(authResponse.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => 
                _authService.AuthenticateAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldThrowException_WhenEmailNotVerified()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "unverified@example.com",
                // Use empty password to bypass password verification
               
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                IsEmailVerified = false
            };

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<EmailVerificationRequiredException>(() => 
                _authService.AuthenticateAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewToken_WhenRefreshTokenIsValid()
        {
            // Arrange
            var refreshToken = "valid_refresh_token";
            var ipAddress = "127.0.0.1";
            
            var authResponse = new AuthenticationResponse
            {
                Token = "new_jwt_token",
                RefreshToken = "new_refresh_token",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
            };

            _tokenServiceMock.Setup(t => t.RefreshToken(refreshToken, ipAddress))
                .Returns(authResponse);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(authResponse.Token, result.Token);
            Assert.Equal(authResponse.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldReturnTrue_WhenTokenIsRevoked()
        {
            // Arrange
            var token = "valid_token";
            var ipAddress = "127.0.0.1";
            var reason = "User logout";

            _tokenServiceMock.Setup(t => t.RevokeTokenAsync(token, ipAddress, reason))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.RevokeTokenAsync(token, ipAddress, reason);

            // Assert
            Assert.True(result);
            _tokenServiceMock.Verify(t => t.RevokeTokenAsync(token, ipAddress, reason), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldSendEmail_WhenUserExists()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email
            };
            var resetToken = "reset_token";
            var resetLink = "https://example.com/reset-password?email=test%40example.com&token=reset_token";

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GeneratePasswordResetTokenAsync(user.Id))
                .ReturnsAsync(resetToken);
            _configurationMock.Setup(c => c["FrontendUrl"])
                .Returns("https://example.com");
            _notificationServiceMock.Setup(n => n.SendResetPasswordEmailAsync(email, resetLink))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ForgotPasswordAsync(email);

            // Assert
            Assert.True(result);
            _notificationServiceMock.Verify(n => n.SendResetPasswordEmailAsync(email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldReturnTrue_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.ForgotPasswordAsync(email);

            // Assert
            Assert.True(result); // Should return true for security reasons
            _notificationServiceMock.Verify(n => n.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnTrue_WhenTokenIsValid()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid_token";
            var newPassword = "NewPassword123!";
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = email
            };

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.ValidatePasswordResetTokenAsync(token))
                .ReturnsAsync(userId);
            _unitOfWorkMock.Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            Assert.True(result);
            _unitOfWorkMock.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var token = "valid_token";
            var newPassword = "NewPassword123!";

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            Assert.False(result);
            _unitOfWorkMock.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnFalse_WhenTokenIsInvalid()
        {
            // Arrange
            var email = "test@example.com";
            var token = "invalid_token";
            var newPassword = "NewPassword123!";
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = email
            };

            _unitOfWorkMock.Setup(u => u.Users.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.ValidatePasswordResetTokenAsync(token))
                .ReturnsAsync(differentUserId); // Different user ID

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            Assert.False(result);
            _unitOfWorkMock.Verify(u => u.Users.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task SendVerificationEmailAsync_ShouldReturnTrue_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _notificationServiceMock.Setup(n => n.SendVerificationEmailAsync(
                userId, user.Email, $"{user.FirstName} {user.LastName}"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SendVerificationEmailAsync(userId);

            // Assert
            Assert.True(result);
            _notificationServiceMock.Verify(n => n.SendVerificationEmailAsync(
                userId, user.Email, $"{user.FirstName} {user.LastName}"), Times.Once);
        }

        [Fact]
        public async Task SendVerificationEmailAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.SendVerificationEmailAsync(userId);

            // Assert
            Assert.False(result);
            _notificationServiceMock.Verify(n => n.SendVerificationEmailAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
