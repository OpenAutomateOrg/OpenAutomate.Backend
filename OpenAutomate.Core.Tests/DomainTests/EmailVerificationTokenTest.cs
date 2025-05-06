using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class EmailVerificationTokenTest
    {
        [Fact]
        public void EmailVerificationToken_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var token = new EmailVerificationToken();

            // Assert
            Assert.NotNull(token);
            Assert.Equal(string.Empty, token.Token);
            Assert.Equal(Guid.Empty, token.UserId);
            Assert.Equal(DateTime.MinValue, token.ExpiresAt);
            Assert.False(token.IsUsed);
            Assert.Null(token.UsedAt);
            Assert.True(token.IsExpired); // Thay đổi kỳ vọng để phù hợp với giá trị mặc định của ExpiresAt
            Assert.False(token.IsActive); // Thay đổi kỳ vọng để phù hợp với logic IsActive
        }

        [Fact]
        public void EmailVerificationToken_IsExpired_ReturnsTrueForExpiredToken()
        {
            // Arrange
            var token = new EmailVerificationToken
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var isExpired = token.IsExpired;

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void EmailVerificationToken_IsExpired_ReturnsFalseForValidToken()
        {
            // Arrange
            var token = new EmailVerificationToken
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            // Act
            var isExpired = token.IsExpired;

            // Assert
            Assert.False(isExpired);
        }
        [Fact]
        public void EmailVerificationToken_IsActive_ReturnsTrueForValidAndUnusedToken()
        {
            // Arrange
            var token = new EmailVerificationToken
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            // Act
            var isActive = token.IsActive;

            // Assert
            Assert.True(isActive);
        }

        [Fact]
        public void EmailVerificationToken_IsActive_ReturnsFalseForExpiredToken()
        {
            // Arrange
            var token = new EmailVerificationToken
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var isActive = token.IsActive;

            // Assert
            Assert.False(isActive);
        }

        [Fact]
        public void EmailVerificationToken_IsActive_ReturnsFalseForUsedToken()
        {
            // Arrange
            var token = new EmailVerificationToken
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = true
            };

            // Act
            var isActive = token.IsActive;

            // Assert
            Assert.False(isActive);
        }
        [Fact]
        public void EmailVerificationToken_LinkUser_UserIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var token = new EmailVerificationToken { User = user };

            // Act
            var linkedUser = token.User;

            // Assert
            Assert.NotNull(linkedUser);
            Assert.Equal("John", linkedUser.FirstName);
            Assert.Equal("Doe", linkedUser.LastName);
        }
        [Fact]
        public void EmailVerificationToken_SetUsedAt_UsedAtIsSet()
        {
            // Arrange
            var token = new EmailVerificationToken();
            var usedAt = DateTime.UtcNow;

            // Act
            token.UsedAt = usedAt;

            // Assert
            Assert.Equal(usedAt, token.UsedAt);
        }

    }
}
