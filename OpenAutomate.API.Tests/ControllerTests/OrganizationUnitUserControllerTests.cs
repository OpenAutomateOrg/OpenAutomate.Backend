using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.IServices;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class OrganizationUnitUserControllerTests
    {
        private readonly Mock<IOrganizationUnitUserService> _mockService;
        private readonly OrganizationUnitUserController _controller;
        private readonly Guid _currentUserId;
        private readonly string _tenantSlug;

        public OrganizationUnitUserControllerTests()
        {
            _mockService = new Mock<IOrganizationUnitUserService>();
            _controller = new OrganizationUnitUserController(_mockService.Object);
            _currentUserId = Guid.NewGuid();
            _tenantSlug = "test-tenant";

            // Setup HttpContext with authenticated user
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = _currentUserId };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetUsersInOrganizationUnit_ReturnsOkResultWithUsers()
        {
            // Arrange
            var users = new List<OrganizationUnitUserDetailDto>
            {
                new OrganizationUnitUserDetailDto
                {
                    UserId = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "User",
                    LastName = "One",
                    Roles = new List<string> { "Admin" },
                    JoinedAt = DateTime.UtcNow.AddDays(-10)
                },
                new OrganizationUnitUserDetailDto
                {
                    UserId = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "User",
                    LastName = "Two",
                    Roles = new List<string> { "User" },
                    JoinedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            _mockService.Setup(s => s.GetUsersInOrganizationUnitAsync(_tenantSlug))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsersInOrganizationUnit(_tenantSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<OrganizationUnitUsersResponseDto>(okResult.Value);
            Assert.Equal(2, response.Count);
            Assert.Equal(2, response.Users.Count());
            Assert.Contains(response.Users, u => u.Email == "user1@example.com");
            Assert.Contains(response.Users, u => u.Email == "user2@example.com");
        }

        [Fact]
        public async Task GetUsersInOrganizationUnit_WithEmptyUserList_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            var users = new List<OrganizationUnitUserDetailDto>();

            _mockService.Setup(s => s.GetUsersInOrganizationUnitAsync(_tenantSlug))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsersInOrganizationUnit(_tenantSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<OrganizationUnitUsersResponseDto>(okResult.Value);
            Assert.Equal(0, response.Count);
            Assert.Empty(response.Users);
        }

        [Fact]
        public async Task DeleteUser_WithValidIdAndNotCurrentUser_ReturnsNoContent()
        {
            // Arrange
            var userIdToDelete = Guid.NewGuid();
            
            _mockService.Setup(s => s.DeleteUserAsync(_tenantSlug, userIdToDelete))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(_tenantSlug, userIdToDelete);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_WithCurrentUserId_ReturnsBadRequest()
        {
            // Arrange - attempt to delete the current user
            
            // Act
            var result = await _controller.DeleteUser(_tenantSlug, _currentUserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            
            // Verify service was not called
            _mockService.Verify(s => s.DeleteUserAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            
            _mockService.Setup(s => s.DeleteUserAsync(_tenantSlug, nonExistentUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUser(_tenantSlug, nonExistentUserId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var userIdToDelete = Guid.NewGuid();
            
            _mockService.Setup(s => s.DeleteUserAsync(_tenantSlug, userIdToDelete))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeleteUser(_tenantSlug, userIdToDelete);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetRolesInOrganizationUnit_ReturnsOkResultWithRoles()
        {
            // Arrange
            var roles = new List<AuthorityDto>
            {
                new AuthorityDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Description = "Administrator role",
                    Permissions = new List<ResourcePermissionDto>
                    {
                        new ResourcePermissionDto
                        {
                            ResourceName = Resources.UserResource,
                            Permission = Permissions.View,
                            PermissionDescription = "View users",
                            ResourceDisplayName = "Users"
                        }
                    }
                },
                new AuthorityDto
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    Description = "Regular user role",
                    Permissions = new List<ResourcePermissionDto>
                    {
                        new ResourcePermissionDto
                        {
                            ResourceName = Resources.AssetResource,
                            Permission = Permissions.View,
                            PermissionDescription = "View assets",
                            ResourceDisplayName = "Assets"
                        }
                    }
                }
            };

            _mockService.Setup(s => s.GetRolesInOrganizationUnitAsync(_tenantSlug))
                .ReturnsAsync(roles);

            // Act
            var result = await _controller.GetRolesInOrganizationUnit(_tenantSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRoles = Assert.IsAssignableFrom<IEnumerable<AuthorityDto>>(okResult.Value);
            Assert.Equal(2, returnedRoles.Count());
            Assert.Contains(returnedRoles, r => r.Name == "Admin");
            Assert.Contains(returnedRoles, r => r.Name == "User");
            
            // Verify permissions
            var adminRole = returnedRoles.First(r => r.Name == "Admin");
            Assert.NotNull(adminRole.Permissions);
            Assert.Single(adminRole.Permissions);
            Assert.Equal(Resources.UserResource, adminRole.Permissions.First().ResourceName);
            
            var userRole = returnedRoles.First(r => r.Name == "User");
            Assert.NotNull(userRole.Permissions);
            Assert.Single(userRole.Permissions);
            Assert.Equal(Resources.AssetResource, userRole.Permissions.First().ResourceName);
        }

        [Fact]
        public async Task GetRolesInOrganizationUnit_WithEmptyRolesList_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            var roles = new List<AuthorityDto>();

            _mockService.Setup(s => s.GetRolesInOrganizationUnitAsync(_tenantSlug))
                .ReturnsAsync(roles);

            // Act
            var result = await _controller.GetRolesInOrganizationUnit(_tenantSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRoles = Assert.IsAssignableFrom<IEnumerable<AuthorityDto>>(okResult.Value);
            Assert.Empty(returnedRoles);
        }
    }
} 