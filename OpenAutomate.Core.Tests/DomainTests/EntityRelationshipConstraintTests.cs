using System;
using OpenAutomate.Core.Domain.Entities;
using Xunit;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class EntityRelationshipConstraintTests
    {
        [Fact]
        public void PasswordResetToken_User_Relationship_Can_Be_Set()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            var token = new PasswordResetToken
            {
                UserId = user.Id,
                User = user,
                Token = "reset-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            Assert.Equal(user.Id, token.UserId);
            Assert.Equal(user, token.User);
        }
    }
} 