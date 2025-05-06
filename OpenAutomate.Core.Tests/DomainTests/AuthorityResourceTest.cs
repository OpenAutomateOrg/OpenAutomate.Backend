using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class AuthorityResourceTest
    {
        [Fact]
        public void AuthorityResource_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var authorityResource = new AuthorityResource();

            // Assert
            Assert.NotNull(authorityResource);
            Assert.Equal(Guid.Empty, authorityResource.AuthorityId);
            Assert.Equal(string.Empty, authorityResource.ResourceName);
            Assert.Equal(0, authorityResource.Permission);
            Assert.Null(authorityResource.Authority);
        }
        [Fact]
        public void AuthorityResource_SetAuthorityId_AuthorityIdIsSet()
        {
            // Arrange
            var authorityResource = new AuthorityResource();
            var authorityId = Guid.NewGuid();

            // Act
            authorityResource.AuthorityId = authorityId;

            // Assert
            Assert.Equal(authorityId, authorityResource.AuthorityId);
        }
        [Fact]
        public void AuthorityResource_SetResourceName_ResourceNameIsSet()
        {
            // Arrange
            var authorityResource = new AuthorityResource();
            var resourceName = "Test Resource";

            // Act
            authorityResource.ResourceName = resourceName;

            // Assert
            Assert.Equal(resourceName, authorityResource.ResourceName);
        }
        [Fact]
        public void AuthorityResource_SetPermission_PermissionIsSet()
        {
            // Arrange
            var authorityResource = new AuthorityResource();
            var permission = 1;

            // Act
            authorityResource.Permission = permission;

            // Assert
            Assert.Equal(permission, authorityResource.Permission);
        }
        [Fact]
        public void AuthorityResource_LinkAuthority_AuthorityIsLinked()
        {
            // Arrange
            var authority = new Authority { Name = "Admin", Description = "Administrator Role" };
            var authorityResource = new AuthorityResource { Authority = authority };

            // Act
            var linkedAuthority = authorityResource.Authority;

            // Assert
            Assert.NotNull(linkedAuthority);
            Assert.Equal("Admin", linkedAuthority.Name);
            Assert.Equal("Administrator Role", linkedAuthority.Description);
        }

    }
}
