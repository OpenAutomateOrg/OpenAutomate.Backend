using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Entities;
using Xunit;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class PasswordResetTokenTests
    {
        [Fact]
        public void Can_Create_Valid_PasswordResetToken()
        {
            var token = new PasswordResetToken
            {
                UserId = Guid.NewGuid(),
                Token = "reset-token",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                IsUsed = false
            };
            Assert.Equal("reset-token", token.Token);
            Assert.False(token.IsUsed);
        }

        [Fact]
        public void Validation_Fails_If_Required_Fields_Missing()
        {
            var token = new PasswordResetToken();
            var context = new ValidationContext(token);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(token, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Token"));
            
        }

        [Fact]
        public void IsExpired_Returns_True_If_Expired()
        {
            var token = new PasswordResetToken
            {
                UserId = Guid.NewGuid(),
                Token = "reset-token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                IsUsed = false
            };
            Assert.True(token.IsExpired);
        }

        [Fact]
        public void IsActive_Returns_False_If_Used_Or_Expired()
        {
            var token = new PasswordResetToken
            {
                UserId = Guid.NewGuid(),
                Token = "reset-token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                IsUsed = false
            };
            Assert.False(token.IsActive);

            token = new PasswordResetToken
            {
                UserId = Guid.NewGuid(),
                Token = "reset-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = true
            };
            Assert.False(token.IsActive);
        }
    }
} 