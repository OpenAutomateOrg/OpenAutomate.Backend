using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class RefreshTokenTest
    {
        [Fact]
        public void RefreshToken_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var refreshToken = new RefreshToken();

            // Assert
            Assert.NotNull(refreshToken);
            Assert.Equal(string.Empty, refreshToken.Token);
            Assert.Equal(string.Empty, refreshToken.CreatedByIp);
            Assert.Null(refreshToken.Revoked);
            Assert.Null(refreshToken.RevokedByIp);
            Assert.Null(refreshToken.ReplacedByToken);
            Assert.Null(refreshToken.ReasonRevoked);
            Assert.False(refreshToken.IsRevoked);
            Assert.False(refreshToken.IsExpired);
            Assert.True(refreshToken.IsActive);
        
        }
        [Fact]
        public void RefreshToken_IsExpired_ReturnsTrueForExpiredToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Expires = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var isExpired = refreshToken.IsExpired;

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void RefreshToken_IsExpired_ReturnsFalseForValidToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Expires = DateTime.UtcNow.AddMinutes(10)
            };

            // Act
            var isExpired = refreshToken.IsExpired;

            // Assert
            Assert.False(isExpired);
        }
        [Fact]
        public void RefreshToken_IsRevoked_ReturnsTrueForRevokedToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Revoked = DateTime.UtcNow
            };

            // Act
            var isRevoked = refreshToken.IsRevoked;

            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public void RefreshToken_IsRevoked_ReturnsFalseForNonRevokedToken()
        {
            // Arrange
            var refreshToken = new RefreshToken();

            // Act
            var isRevoked = refreshToken.IsRevoked;

            // Assert
            Assert.False(isRevoked);
        }
        [Fact]
        public void RefreshToken_IsActive_ReturnsTrueForValidAndNonRevokedToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Expires = DateTime.UtcNow.AddMinutes(10)
            };

            // Act
            var isActive = refreshToken.IsActive;

            // Assert
            Assert.True(isActive);
        }

        [Fact]
        public void RefreshToken_IsActive_ReturnsFalseForExpiredToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Expires = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var isActive = refreshToken.IsActive;

            // Assert
            Assert.False(isActive);
        }

        [Fact]
        public void RefreshToken_IsActive_ReturnsFalseForRevokedToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Expires = DateTime.UtcNow.AddMinutes(10),
                Revoked = DateTime.UtcNow
            };

            // Act
            var isActive = refreshToken.IsActive;

            // Assert
            Assert.False(isActive);
        }
        [Fact]
        public void RefreshToken_LinkUser_UserIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var refreshToken = new RefreshToken { User = user };

            // Act
            var linkedUser = refreshToken.User;

            // Assert
            Assert.NotNull(linkedUser);
            Assert.Equal("John", linkedUser.FirstName);
            Assert.Equal("Doe", linkedUser.LastName);
        }

    }
}
