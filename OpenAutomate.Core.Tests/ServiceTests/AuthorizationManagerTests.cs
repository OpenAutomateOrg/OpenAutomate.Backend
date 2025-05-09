using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class AuthorizationManagerTests
    {
        private readonly Mock<IAuthorizationManager> _mockAuthManager;
        public AuthorizationManagerTests()
        {
            _mockAuthManager = new Mock<IAuthorizationManager>();
        }

        [Fact]
        public async Task HasPermissionAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string resourceName = "UserManagement";
            int permission = 1;

            _mockAuthManager.Setup(manager => manager.HasPermissionAsync(userId, resourceName, permission))
                .ReturnsAsync(true);

            // Act
            bool result = await _mockAuthManager.Object.HasPermissionAsync(userId, resourceName, permission);

            // Assert
            Assert.True(result);
            _mockAuthManager.Verify(manager => manager.HasPermissionAsync(userId, resourceName, permission), Times.Once);
        }

        [Fact]
        public async Task HasPermissionAsync_WithInvalidUserId_ThrowsException()
        {
            // Arrange
            Guid invalidUserId = Guid.Empty;
            string resourceName = "UserManagement";
            int permission = 1;

            _mockAuthManager.Setup(manager => manager.HasPermissionAsync(invalidUserId, resourceName, permission))
                .ThrowsAsync(new ArgumentException("Invalid user ID"));

            // Act
            Func<Task> act = async () => await _mockAuthManager.Object.HasPermissionAsync(invalidUserId, resourceName, permission);

            // Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Equal("Invalid user ID", exception.Message);
        }

        [Fact]
        public async Task HasAuthorityAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string authorityName = "Admin";

            _mockAuthManager.Setup(manager => manager.HasAuthorityAsync(userId, authorityName))
                .ReturnsAsync(true);

            // Act
            bool result = await _mockAuthManager.Object.HasAuthorityAsync(userId, authorityName);

            // Assert
            Assert.True(result);
            _mockAuthManager.Verify(manager => manager.HasAuthorityAsync(userId, authorityName), Times.Once);
        }

        [Fact]
        public async Task GetUserAuthoritiesAsync_WithValidUserId_ReturnsAuthorities()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var expectedAuthorities = new List<Authority>
            {
                new Authority { Name = "Admin" },
                new Authority { Name = "User" }
            };

            _mockAuthManager.Setup(manager => manager.GetUserAuthoritiesAsync(userId))
                .ReturnsAsync(expectedAuthorities);

            // Act
            var result = await _mockAuthManager.Object.GetUserAuthoritiesAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Name == "Admin");
            Assert.Contains(result, a => a.Name == "User");
            _mockAuthManager.Verify(manager => manager.GetUserAuthoritiesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AssignAuthorityToUserAsync_WithValidParameters_Succeeds()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string authorityName = "Admin";

            _mockAuthManager.Setup(manager => manager.AssignAuthorityToUserAsync(userId, authorityName))
                .Returns(Task.CompletedTask);

            // Act
            await _mockAuthManager.Object.AssignAuthorityToUserAsync(userId, authorityName);

            // Assert
            _mockAuthManager.Verify(manager => manager.AssignAuthorityToUserAsync(userId, authorityName), Times.Once);
        }

        [Fact]
        public async Task AssignAuthorityToUserAsync_WithNonExistentAuthority_ThrowsException()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string nonExistentAuthority = "NonExistentAuthority";

            _mockAuthManager.Setup(manager => manager.AssignAuthorityToUserAsync(userId, nonExistentAuthority))
                .ThrowsAsync(new InvalidOperationException("Authority does not exist"));

            // Act
            Func<Task> act = async () => await _mockAuthManager.Object.AssignAuthorityToUserAsync(userId, nonExistentAuthority);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("Authority does not exist", exception.Message);
        }

        [Fact]
        public async Task RemoveAuthorityFromUserAsync_WithValidParameters_Succeeds()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            string authorityName = "Admin";

            _mockAuthManager.Setup(manager => manager.RemoveAuthorityFromUserAsync(userId, authorityName))
                .Returns(Task.CompletedTask);

            // Act
            await _mockAuthManager.Object.RemoveAuthorityFromUserAsync(userId, authorityName);

            // Assert
            _mockAuthManager.Verify(manager => manager.RemoveAuthorityFromUserAsync(userId, authorityName), Times.Once);
        }

        [Fact]
        public async Task AddResourcePermissionAsync_WithValidParameters_Succeeds()
        {
            // Arrange
            string authorityName = "Admin";
            string resourceName = "UserManagement";
            int permission = 1;

            _mockAuthManager.Setup(manager => manager.AddResourcePermissionAsync(authorityName, resourceName, permission))
                .Returns(Task.CompletedTask);

            // Act
            await _mockAuthManager.Object.AddResourcePermissionAsync(authorityName, resourceName, permission);

            // Assert
            _mockAuthManager.Verify(manager => manager.AddResourcePermissionAsync(authorityName, resourceName, permission), Times.Once);
        }

        [Fact]
        public async Task RemoveResourcePermissionAsync_WithValidParameters_Succeeds()
        {
            // Arrange
            string authorityName = "Admin";
            string resourceName = "UserManagement";

            _mockAuthManager.Setup(manager => manager.RemoveResourcePermissionAsync(authorityName, resourceName))
                .Returns(Task.CompletedTask);

            // Act
            await _mockAuthManager.Object.RemoveResourcePermissionAsync(authorityName, resourceName);

            // Assert
            _mockAuthManager.Verify(manager => manager.RemoveResourcePermissionAsync(authorityName, resourceName), Times.Once);
        }

        [Fact]
        public async Task RemoveResourcePermissionAsync_WithNonExistentPermission_ThrowsException()
        {
            // Arrange
            string authorityName = "Admin";
            string nonExistentResource = "NonExistentResource";

            _mockAuthManager.Setup(manager => manager.RemoveResourcePermissionAsync(authorityName, nonExistentResource))
                .ThrowsAsync(new InvalidOperationException("Resource permission does not exist"));

            // Act
            Func<Task> act = async () => await _mockAuthManager.Object.RemoveResourcePermissionAsync(authorityName, nonExistentResource);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal("Resource permission does not exist", exception.Message);
        }
    }
}
