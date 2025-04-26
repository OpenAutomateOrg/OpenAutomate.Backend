using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class UserTests
    {
        [Fact]
        public void User_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var user = new User();
            
            // Assert
            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.False(user.IsEmailVerified);
            Assert.NotNull(user.OrganizationUnitUsers);
            Assert.NotNull(user.RefreshTokens);
        }
        
        [Fact]
        public void User_AddRefreshToken_TokenIsAdded()
        {
            // Arrange
            var user = new User();
            var token = new RefreshToken
            {
                Token = "test-token",
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            
            // Act
            user.RefreshTokens.Add(token);
            
            // Assert
            Assert.Contains(token, user.RefreshTokens);
            Assert.Single(user.RefreshTokens);
        }
        
        [Fact]
        public void User_HasManyOrganizationUnits_RelationshipWorks()
        {
            // Arrange
            var user = new User();
            var org1 = new OrganizationUnit { Name = "Org 1" };
            var org2 = new OrganizationUnit { Name = "Org 2" };
            
            // Act
            user.OrganizationUnitUsers = new List<OrganizationUnitUser>
            {
                new OrganizationUnitUser { OrganizationUnit = org1, User = user },
                new OrganizationUnitUser { OrganizationUnit = org2, User = user }
            };
            
            // Assert
            Assert.Equal(2, user.OrganizationUnitUsers.Count);
            Assert.Contains(user.OrganizationUnitUsers, o => o.OrganizationUnit.Name == "Org 1");
            Assert.Contains(user.OrganizationUnitUsers, o => o.OrganizationUnit.Name == "Org 2");
        }
        
        [Fact]
        public void User_RefreshTokens_CanDetectExpiredTokens()
        {
            // Arrange
            var user = new User();
            var expiredToken = new RefreshToken
            {
                Token = "expired-token",
                Created = DateTime.UtcNow.AddDays(-10),
                Expires = DateTime.UtcNow.AddDays(-3)
            };
            var validToken = new RefreshToken
            {
                Token = "valid-token",
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            user.RefreshTokens.Add(expiredToken);
            user.RefreshTokens.Add(validToken);
            
            // Act - We would typically have a helper method for this in the User class
            var expiredTokens = user.RefreshTokens.Where(t => t.Expires < DateTime.UtcNow).ToList();
            var activeTokens = user.RefreshTokens.Where(t => t.Expires >= DateTime.UtcNow).ToList();
            
            // Assert
            Assert.Single(expiredTokens);
            Assert.Equal("expired-token", expiredTokens[0].Token);
            Assert.Single(activeTokens);
            Assert.Equal("valid-token", activeTokens[0].Token);
        }
    }
} 