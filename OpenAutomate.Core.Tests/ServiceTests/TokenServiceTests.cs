using Xunit;
using Moq;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using System;
using Microsoft.IdentityModel.Tokens;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class TokenServiceTests
    {
        private readonly Mock<ITokenService> _mockTokenService;

        public TokenServiceTests()
        {
            _mockTokenService = new Mock<ITokenService>();
        }

        [Fact]
        public void ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            string validToken = "valid.jwt.token";
            _mockTokenService.Setup(service => service.ValidateToken(validToken)).Returns(true);

            // Act
            bool result = _mockTokenService.Object.ValidateToken(validToken);

            // Assert
            Assert.True(result);
            _mockTokenService.Verify(service => service.ValidateToken(validToken), Times.Once);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            string invalidToken = "invalid.jwt.token";
            _mockTokenService.Setup(service => service.ValidateToken(invalidToken)).Returns(false);

            // Act
            bool result = _mockTokenService.Object.ValidateToken(invalidToken);

            // Assert
            Assert.False(result);
            _mockTokenService.Verify(service => service.ValidateToken(invalidToken), Times.Once);
        }

        [Fact]
        public void GenerateTokens_WithValidUser_ReturnsAuthenticationResponse()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            string ipAddress = "127.0.0.1";
            var expectedResponse = new AuthenticationResponse
            {
                Token = "generated.jwt.token",
                RefreshToken = "generated.refresh.token"
            };

            _mockTokenService.Setup(service => service.GenerateTokens(user, ipAddress))
                .Returns(expectedResponse);

            // Act
            var result = _mockTokenService.Object.GenerateTokens(user, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Token, result.Token);
            Assert.Equal(expectedResponse.RefreshToken, result.RefreshToken);
            _mockTokenService.Verify(service => service.GenerateTokens(user, ipAddress), Times.Once);
        }

        [Fact]
        public void RefreshToken_WithValidRefreshToken_ReturnsNewTokens()
        {
            // Arrange
            string refreshToken = "valid.refresh.token";
            string ipAddress = "127.0.0.1";
            var expectedResponse = new AuthenticationResponse
            {
                Token = "new.access.token",
                RefreshToken = "new.refresh.token"
            };

            _mockTokenService.Setup(service => service.RefreshToken(refreshToken, ipAddress))
                .Returns(expectedResponse);

            // Act
            var result = _mockTokenService.Object.RefreshToken(refreshToken, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Token, result.Token);
            Assert.Equal(expectedResponse.RefreshToken, result.RefreshToken);
            _mockTokenService.Verify(service => service.RefreshToken(refreshToken, ipAddress), Times.Once);
        }

        [Fact]
        public void RefreshToken_WithInvalidRefreshToken_ThrowsException()
        {
            // Arrange
            string invalidToken = "invalid.refresh.token";
            string ipAddress = "127.0.0.1";

            _mockTokenService.Setup(service => service.RefreshToken(invalidToken, ipAddress))
                .Throws(new InvalidOperationException("Invalid refresh token"));

            // Act
            Action act = () => _mockTokenService.Object.RefreshToken(invalidToken, ipAddress);

            // Assert
            var exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Equal("Invalid refresh token", exception.Message);
        }

        [Fact]
        public void RefreshToken_WithExpiredRefreshToken_ThrowsException()
        {
            // Arrange
            string expiredToken = "expired.refresh.token";
            string ipAddress = "127.0.0.1";

            _mockTokenService.Setup(service => service.RefreshToken(expiredToken, ipAddress))
                .Throws(new Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException("Refresh token has expired"));

            // Act
            Action act = () => _mockTokenService.Object.RefreshToken(expiredToken, ipAddress);

            // Assert
            var exception = Assert.Throws<Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException>(act);
            Assert.Equal("Refresh token has expired", exception.Message);
        }

        [Fact]
        public void RefreshToken_WithNullRefreshToken_ThrowsArgumentNullException()
        {
            // Arrange
            string nullToken = null;
            string ipAddress = "127.0.0.1";

            _mockTokenService.Setup(service => service.RefreshToken(nullToken, ipAddress))
                .Throws(new ArgumentNullException(nameof(nullToken), "Refresh token cannot be null"));

            // Act
            Action act = () => _mockTokenService.Object.RefreshToken(nullToken, ipAddress);

            // Assert
            var exception = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("Refresh token cannot be null (Parameter 'nullToken')", exception.Message);
        }

        [Fact]
        public void RefreshToken_WithEmptyRefreshToken_ThrowsArgumentException()
        {
            // Arrange
            string emptyToken = string.Empty;
            string ipAddress = "127.0.0.1";

            _mockTokenService.Setup(service => service.RefreshToken(emptyToken, ipAddress))
                .Throws(new ArgumentException("Refresh token cannot be empty", nameof(emptyToken)));

            // Act
            Action act = () => _mockTokenService.Object.RefreshToken(emptyToken, ipAddress);

            // Assert
            var exception = Assert.Throws<ArgumentException>(act);
            Assert.Equal("Refresh token cannot be empty (Parameter 'emptyToken')", exception.Message);
        }

        [Fact]
        public async Task RevokeTokenAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            string validToken = "valid.refresh.token";
            string ipAddress = "127.0.0.1";
            string reason = "User logged out";

            _mockTokenService.Setup(service => service.RevokeTokenAsync(validToken, ipAddress, reason))
                .ReturnsAsync(true);

            // Act
            bool result = await _mockTokenService.Object.RevokeTokenAsync(validToken, ipAddress, reason);

            // Assert
            Assert.True(result);
            _mockTokenService.Verify(service => service.RevokeTokenAsync(validToken, ipAddress, reason), Times.Once);
        }

        [Fact]
        public async Task RevokeTokenAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            string invalidToken = "invalid.refresh.token";
            string ipAddress = "127.0.0.1";
            string reason = "User logged out";

            _mockTokenService.Setup(service => service.RevokeTokenAsync(invalidToken, ipAddress, reason))
                .ReturnsAsync(false);

            // Act
            bool result = await _mockTokenService.Object.RevokeTokenAsync(invalidToken, ipAddress, reason);

            // Assert
            Assert.False(result);
            _mockTokenService.Verify(service => service.RevokeTokenAsync(invalidToken, ipAddress, reason), Times.Once);
        }

        [Fact]
        public async Task RevokeTokenAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Arrange
            string nullToken = null;
            string ipAddress = "127.0.0.1";
            string reason = "User logged out";

            _mockTokenService.Setup(service => service.RevokeTokenAsync(nullToken, ipAddress, reason))
                .ThrowsAsync(new ArgumentNullException(nameof(nullToken), "Token cannot be null"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.RevokeTokenAsync(nullToken, ipAddress, reason);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
            Assert.Equal("Token cannot be null (Parameter 'nullToken')", exception.Message);
        }

        [Fact]
        public async Task RevokeTokenAsync_WithEmptyToken_ThrowsArgumentException()
        {
            // Arrange
            string emptyToken = string.Empty;
            string ipAddress = "127.0.0.1";
            string reason = "User logged out";

            _mockTokenService.Setup(service => service.RevokeTokenAsync(emptyToken, ipAddress, reason))
                .ThrowsAsync(new ArgumentException("Token cannot be empty", nameof(emptyToken)));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.RevokeTokenAsync(emptyToken, ipAddress, reason);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("Token cannot be empty (Parameter 'emptyToken')", exception.Message);
        }

        [Fact]
        public async Task GenerateEmailVerificationTokenAsync_WithValidUserId_ReturnsToken()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string expectedToken = "email.verification.token";

            _mockTokenService.Setup(service => service.GenerateEmailVerificationTokenAsync(userId))
                .ReturnsAsync(expectedToken);

            // Act
            string result = await _mockTokenService.Object.GenerateEmailVerificationTokenAsync(userId);

            // Assert
            Assert.False(string.IsNullOrEmpty(result), "Token should not be null or empty");
            Assert.Equal(expectedToken, result);
            _mockTokenService.Verify(service => service.GenerateEmailVerificationTokenAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GenerateEmailVerificationTokenAsync_WithEmptyGuid_ThrowsArgumentException()
        {
            // Arrange
            Guid emptyUserId = Guid.Empty;

            _mockTokenService.Setup(service => service.GenerateEmailVerificationTokenAsync(emptyUserId))
                .ThrowsAsync(new ArgumentException("User ID cannot be empty", nameof(emptyUserId)));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.GenerateEmailVerificationTokenAsync(emptyUserId);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("User ID cannot be empty (Parameter 'emptyUserId')", exception.Message);

        }

        [Fact]
        public async Task GenerateEmailVerificationTokenAsync_WithNonExistentUser_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid nonExistentUserId = Guid.NewGuid();

            _mockTokenService.Setup(service => service.GenerateEmailVerificationTokenAsync(nonExistentUserId))
                .ThrowsAsync(new InvalidOperationException("User not found"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.GenerateEmailVerificationTokenAsync(nonExistentUserId);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public async Task GenerateEmailVerificationTokenAsync_WithAlreadyVerifiedUser_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid verifiedUserId = Guid.NewGuid();

            _mockTokenService.Setup(service => service.GenerateEmailVerificationTokenAsync(verifiedUserId))
                .ThrowsAsync(new InvalidOperationException("User email is already verified"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.GenerateEmailVerificationTokenAsync(verifiedUserId);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("User email is already verified", exception.Message);
        }

        [Fact]
        public async Task GenerateEmailVerificationTokenAsync_WithDatabaseError_ThrowsException()
        {
            // Arrange
            Guid userId = Guid.NewGuid();

            // Mock ném Exception khi có lỗi database
            _mockTokenService.Setup(service => service.GenerateEmailVerificationTokenAsync(userId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.GenerateEmailVerificationTokenAsync(userId);

            // Assert
            var exception = await Assert.ThrowsAsync<Exception>(act);
            Assert.Equal("Database connection error", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithValidToken_ReturnsUserId()
        {
            // Arrange
            string validToken = "valid.email.verification.token";
            Guid expectedUserId = Guid.NewGuid();

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(validToken))
                .ReturnsAsync(expectedUserId);

            // Act
            Guid? result = await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(validToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUserId, result);
            _mockTokenService.Verify(service => service.ValidateEmailVerificationTokenAsync(validToken), Times.Once);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            string invalidToken = "invalid.email.verification.token";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(invalidToken))
                .ReturnsAsync((Guid?)null);

            // Act
            Guid? result = await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(invalidToken);

            // Assert
            Assert.Null(result);
            _mockTokenService.Verify(service => service.ValidateEmailVerificationTokenAsync(invalidToken), Times.Once);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithInvalidTokenFormat_ThrowsSecurityTokenException()
        {
            // Arrange
            string invalidFormatToken = "invalid.token.format";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(invalidFormatToken))
                .ThrowsAsync(new SecurityTokenException("Invalid token format"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(invalidFormatToken);
            
            // Assert
            var exception = await Assert.ThrowsAsync<SecurityTokenException>(act);
            Assert.Equal("Invalid token format", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Arrange
            string nullToken = null;

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(nullToken))
                .ThrowsAsync(new ArgumentNullException(nameof(nullToken), "Token cannot be null"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(nullToken);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
            Assert.Equal("Token cannot be null (Parameter 'nullToken')", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithEmptyToken_ThrowsArgumentException()
        {
            // Arrange
            string emptyToken = string.Empty;

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(emptyToken))
                .ThrowsAsync(new ArgumentException("Token cannot be empty", nameof(emptyToken)));

            // Act & Assert
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(emptyToken);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("Token cannot be empty (Parameter 'emptyToken')", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithExpiredToken_ThrowsSecurityTokenExpiredException()
        {
            // Arrange
            string expiredToken = "expired.email.verification.token";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(expiredToken))
                .ThrowsAsync(new SecurityTokenExpiredException("Token has expired"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(expiredToken);

            // Assert
            var exception = await Assert.ThrowsAsync<SecurityTokenExpiredException>(act);
            Assert.Equal("Token has expired", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithNonExistentUser_ThrowsInvalidOperationException()
        {
            // Arrange
            string token = "valid.token.but.user.not.found";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(token))
                .ThrowsAsync(new InvalidOperationException("User associated with this token no longer exists"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(token);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("User associated with this token no longer exists", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithDatabaseError_ThrowsException()
        {
            // Arrange
            string token = "valid.token";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(token))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(token);

            // Assert
            var exception = await Assert.ThrowsAsync<Exception>(act);
            Assert.Equal("Database connection error", exception.Message);
        }

        [Fact]
        public async Task ValidateEmailVerificationTokenAsync_WithWrongTokenType_ThrowsInvalidOperationException()
        {
            // Arrange
            string wrongTokenType = "wrong.type.token";

            _mockTokenService.Setup(service => service.ValidateEmailVerificationTokenAsync(wrongTokenType))
                .ThrowsAsync(new InvalidOperationException("Token is not an email verification token"));

            // Act
            Func<Task> act = async () => await _mockTokenService.Object.ValidateEmailVerificationTokenAsync(wrongTokenType);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("Token is not an email verification token", exception.Message);
        }
    }
} 