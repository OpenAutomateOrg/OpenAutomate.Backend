using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class UserTests
    {
       
     
        
        [Fact]
        public void User_AddRefreshToken_TokenIsAdded()
        {
            // Arrange
            var user = new User();
            var token = new RefreshToken
            {
                Token = "test-token",
                CreatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Expires = DateTime.UtcNow.AddDays(-3)
            };
            var validToken = new RefreshToken
            {
                Token = "valid-token",
                CreatedAt= DateTime.UtcNow,
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
        [Fact]
        public void User_OwnsToken_ReturnsTrueIfTokenExists()
        {
            // Arrange
            var user = new User();
            var token = new RefreshToken
            {
                Token = "test-token",
                CreatedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            user.RefreshTokens.Add(token);

            // Act
            var ownsToken = user.OwnsToken("test-token");

            // Assert
            Assert.True(ownsToken);
        }

        [Fact]
        public void User_OwnsToken_ReturnsFalseIfTokenDoesNotExist()
        {
            // Arrange
            var user = new User();

            // Act
            var ownsToken = user.OwnsToken("non-existent-token");

            // Assert
            Assert.False(ownsToken);
        }
        [Fact]
        public void User_AddAuthority_AuthorityIsAdded()
        {
            // Arrange
            var user = new User();
            var authority = new UserAuthority
            {
                AuthorityId = Guid.NewGuid(),
                OrganizationUnitId = Guid.NewGuid()
            };

            // Act
            user.Authorities.Add(authority);

            // Assert
            Assert.Contains(authority, user.Authorities);
        }
        [Fact]
        public void User_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            Assert.NotNull(user.RefreshTokens);
            Assert.NotNull(user.OrganizationUnitUsers);
            Assert.NotNull(user.Authorities);
            Assert.False(user.IsEmailVerified);
            Assert.Equal(SystemRole.User, user.SystemRole);
            Assert.Null(user.FirstName);
            Assert.Null(user.LastName);
            Assert.Null(user.ImageUrl);
        }
        [Fact]
        public void User_AddOrganizationUnit_UnitIsAdded()
        {
            // Arrange
            var user = new User();
            var orgUnit = new OrganizationUnit { Name = "Test Organization" };
            var orgUnitUser = new OrganizationUnitUser
            {
                OrganizationUnit = orgUnit,
                User = user
            };

            // Act
            user.OrganizationUnitUsers.Add(orgUnitUser);

            // Assert
            Assert.Contains(orgUnitUser, user.OrganizationUnitUsers);
            Assert.Equal(user, orgUnitUser.User);
            Assert.Equal(orgUnit, orgUnitUser.OrganizationUnit);
        }
        [Fact]
        public void User_UpdateDetails_UpdatesSuccessfully()
        {
            // Arrange
            var user = new User();

            // Act
            user.FirstName = "John";
            user.LastName = "Doe";
            user.ImageUrl = "http://example.com/image.jpg";

            // Assert
            Assert.Equal("John", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.Equal("http://example.com/image.jpg", user.ImageUrl);
        }

    }
} 