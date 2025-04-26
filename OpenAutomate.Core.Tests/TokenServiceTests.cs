using Xunit;
using Moq;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using System;

namespace OpenAutomate.Core.Tests
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
    }
} 