using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class UserAuthorityTest
    {
        [Fact]
        public void UserAuthority_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var userAuthority = new UserAuthority();

            // Assert
            Assert.NotNull(userAuthority);
            Assert.Equal(Guid.Empty, userAuthority.UserId);
            Assert.Equal(Guid.Empty, userAuthority.AuthorityId);
            Assert.Equal(Guid.Empty, userAuthority.OrganizationUnitId);
        }
        [Fact]
        public void UserAuthority_LinkUser_UserIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var userAuthority = new UserAuthority { User = user };

            // Act
            var linkedUser = userAuthority.User;

            // Assert
            Assert.NotNull(linkedUser);
            Assert.Equal("John", linkedUser.FirstName);
            Assert.Equal("Doe", linkedUser.LastName);
        }
        [Fact]
        public void UserAuthority_LinkAuthority_AuthorityIsLinked()
        {
            // Arrange
            var authority = new Authority { Name = "Admin", Description = "Administrator Role" };
            var userAuthority = new UserAuthority { Authority = authority };

            // Act
            var linkedAuthority = userAuthority.Authority;

            // Assert
            Assert.NotNull(linkedAuthority);
            Assert.Equal("Admin", linkedAuthority.Name);
            Assert.Equal("Administrator Role", linkedAuthority.Description);
        }
        [Fact]
        public void UserAuthority_LinkOrganizationUnit_OrganizationUnitIsLinked()
        {
            // Arrange
            var orgUnit = new OrganizationUnit { Name = "Test Organization", Description = "Test Description" };
            var userAuthority = new UserAuthority { OrganizationUnit = orgUnit };

            // Act
            var linkedOrgUnit = userAuthority.OrganizationUnit;

            // Assert
            Assert.NotNull(linkedOrgUnit);
            Assert.Equal("Test Organization", linkedOrgUnit.Name);
            Assert.Equal("Test Description", linkedOrgUnit.Description);
        }
        [Fact]
        public void UserAuthority_SetIds_IdsAreSetCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();
            var orgUnitId = Guid.NewGuid();
            var userAuthority = new UserAuthority
            {
                UserId = userId,
                AuthorityId = authorityId,
                OrganizationUnitId = orgUnitId
            };

            // Act & Assert
            Assert.Equal(userId, userAuthority.UserId);
            Assert.Equal(authorityId, userAuthority.AuthorityId);
            Assert.Equal(orgUnitId, userAuthority.OrganizationUnitId);
        }

    }
}