using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class AuthorityTest
    {
        [Fact]
        public void Authority_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var authority = new Authority();

            // Assert
            Assert.NotNull(authority);
            Assert.Equal(string.Empty, authority.Name);
            Assert.Equal(string.Empty, authority.Description);
            Assert.Null(authority.UserAuthorities);
            Assert.Null(authority.AuthorityResources);
        }
        [Fact]
        public void Authority_SetName_NameIsSet()
        {
            // Arrange
            var authority = new Authority();
            var name = "Admin";

            // Act
            authority.Name = name;

            // Assert
            Assert.Equal(name, authority.Name);
        }
        [Fact]
        public void Authority_SetDescription_DescriptionIsSet()
        {
            // Arrange
            var authority = new Authority();
            var description = "Administrator role with full permissions";

            // Act
            authority.Description = description;

            // Assert
            Assert.Equal(description, authority.Description);
        }
        [Fact]
        public void Authority_AddUserAuthority_UserAuthorityIsAdded()
        {
            // Arrange
            var authority = new Authority { UserAuthorities = new List<UserAuthority>() };
            var userAuthority = new UserAuthority
            {
                UserId = Guid.NewGuid(),
                AuthorityId = Guid.NewGuid(),
                OrganizationUnitId = Guid.NewGuid()
            };

            // Act
            authority.UserAuthorities.Add(userAuthority);

            // Assert
            Assert.NotNull(authority.UserAuthorities);
            Assert.Contains(userAuthority, authority.UserAuthorities);
            Assert.Single(authority.UserAuthorities);
        }
        [Fact]
        public void Authority_AddAuthorityResource_AuthorityResourceIsAdded()
        {
            // Arrange
            var authority = new Authority { AuthorityResources = new List<AuthorityResource>() };
            var authorityResource = new AuthorityResource
            {
                AuthorityId = Guid.NewGuid(),
                ResourceName = "Test Resource",
                Permission = 1
            };

            // Act
            authority.AuthorityResources.Add(authorityResource);

            // Assert
            Assert.NotNull(authority.AuthorityResources);
            Assert.Contains(authorityResource, authority.AuthorityResources);
            Assert.Single(authority.AuthorityResources);
        }

    }
}

