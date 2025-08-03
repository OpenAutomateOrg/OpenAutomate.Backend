using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class AuthorControllerTests
    {
        private readonly Mock<IAuthorizationManager> _mockAuthorizationManager;
        private readonly Mock<IOrganizationUnitService> _mockOrganizationUnitService;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly AuthorController _controller;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        public AuthorControllerTests()
        {
            _mockAuthorizationManager = new Mock<IAuthorizationManager>();
            _mockOrganizationUnitService = new Mock<IOrganizationUnitService>();
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _mockTenantContext = new Mock<ITenantContext>();
            
            _controller = new AuthorController(
                _mockAuthorizationManager.Object, 
                _mockOrganizationUnitService.Object,
                _mockCacheInvalidationService.Object,
                _mockTenantContext.Object);
            
            // Setup controller context
            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues["tenant"] = "test-tenant";
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim("tenant_id", _tenantId.ToString())
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region CreateAuthority Tests

        [Fact]
        public async Task CreateAuthority_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new CreateAuthorityDto
            {
                Name = "TestRole",
                Description = "Test role description"
            };
            
            var expectedResult = new AuthorityDto
            {
                Id = Guid.NewGuid(),
                Name = "TestRole",
                Description = "Test role description"
            };

            _mockAuthorizationManager
    .Setup(x => x.CreateAuthorityAsync(It.IsAny<CreateAuthorityDto>()))
    .ReturnsAsync((CreateAuthorityDto dto) => new AuthorityWithPermissionsDto
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Description = dto.Description,
        Permissions = new List<ResourcePermissionDto>()
    });


            // Act
            var result = await _controller.CreateAuthority(createDto);

            // Assert
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            _mockAuthorizationManager.Verify(x => x.CreateAuthorityAsync(It.Is<CreateAuthorityDto>(dto => 
                dto.Name == createDto.Name && dto.Description == createDto.Description)), Times.Once);
        }

        [Fact]
        public async Task CreateAuthority_WithDuplicateName_ReturnsConflict()
        {
            // Arrange
            var createDto = new CreateAuthorityDto
            {
                Name = "ExistingRole",
                Description = "Test role description"
            };
            
            _mockAuthorizationManager
                .Setup(x => x.CreateAuthorityAsync(It.IsAny<CreateAuthorityDto>()))
                .ThrowsAsync(new InvalidOperationException("Authority with name 'ExistingRole' already exists"));

            // Act
            var result = await _controller.CreateAuthority(createDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);
        }

        [Fact]
        public async Task CreateAuthority_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateAuthorityDto
            {
                Name = string.Empty,
                Description = "Test role description"
            };
            
            _mockAuthorizationManager
                .Setup(x => x.CreateAuthorityAsync(It.IsAny<CreateAuthorityDto>()))
                .ThrowsAsync(new ArgumentException("Authority name cannot be empty"));

            // Act
            var result = await _controller.CreateAuthority(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion

        #region GetAllAuthorities Tests

        [Fact]
        public async Task GetAllAuthorities_ReturnsOkWithAuthorities()
        {
            // Arrange
            var authorities = new List<AuthorityWithPermissionsDto>
            {
                new AuthorityWithPermissionsDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Description = "Administrator role",
                    Permissions = new List<ResourcePermissionDto>
                    {
                        new ResourcePermissionDto
                        {
                            ResourceName = Resources.OrganizationUnitResource,
                            Permission = 4 // Delete permission
                        }
                    }
                },
                new AuthorityWithPermissionsDto
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    Description = "Regular user role",
                    Permissions = new List<ResourcePermissionDto>
                    {
                        new ResourcePermissionDto
                        {
                            ResourceName = Resources.OrganizationUnitResource,
                            Permission = 1 // View permission
                        }
                    }
                }
            };
            
            _mockAuthorizationManager
                .Setup(x => x.GetAllAuthoritiesWithPermissionsAsync())
                .ReturnsAsync(authorities);

            // Act
            var result = await _controller.GetAllAuthorities();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAuthorities = Assert.IsAssignableFrom<IEnumerable<AuthorityWithPermissionsDto>>(okResult.Value);
            Assert.Equal(2, returnedAuthorities.Count());
            Assert.Contains(returnedAuthorities, a => a.Name == "Admin");
            Assert.Contains(returnedAuthorities, a => a.Name == "User");
        }

        #endregion

        #region GetAuthority Tests

        [Fact]
        public async Task GetAuthority_WithValidId_ReturnsOkWithAuthority()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            var authority = new AuthorityWithPermissionsDto
            {
                Id = authorityId,
                Name = "Admin",
                Description = "Administrator role",
                Permissions = new List<ResourcePermissionDto>
                {
                    new ResourcePermissionDto
                    {
                        ResourceName = Resources.OrganizationUnitResource,
                        Permission = 4 // Delete permission
                    }
                }
            };
            
            _mockAuthorizationManager
                .Setup(x => x.GetAuthorityWithPermissionsAsync(authorityId))
                .ReturnsAsync(authority);

            // Act
            var result = await _controller.GetAuthority(authorityId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAuthority = Assert.IsType<AuthorityWithPermissionsDto>(okResult.Value);
            Assert.Equal(authorityId, returnedAuthority.Id);
            Assert.Equal("Admin", returnedAuthority.Name);
        }

        [Fact]
        public async Task GetAuthority_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var authorityId = Guid.NewGuid();

            _mockAuthorizationManager
                .Setup(x => x.GetAuthorityWithPermissionsAsync(authorityId))
                .ReturnsAsync((AuthorityWithPermissionsDto?)null); // Explicitly mark as nullable

            // Act
            var result = await _controller.GetAuthority(authorityId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


        #endregion

        #region UpdateAuthority Tests

        // In AuthorControllerTests.cs, update the test(s) that use UpdateAuthorityDto.Permissions
        // to use UpdateAuthorityDto.ResourcePermissions instead

        [Fact]
        public async Task UpdateAuthority_WithValidData_ReturnsOk()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            var updateDto = new UpdateAuthorityDto
            {
                Name = "UpdatedRole",
                Description = "Updated description",
                ResourcePermissions = new List<CreateResourcePermissionDto>
        {
            new CreateResourcePermissionDto
            {
                ResourceName = Resources.OrganizationUnitResource,
                Permission = 3 // Update permission
            }
        }
            };

            _mockAuthorizationManager
                .Setup(x => x.UpdateAuthorityAsync(authorityId, It.IsAny<UpdateAuthorityDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateAuthority(authorityId, updateDto);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.UpdateAuthorityAsync(
                authorityId,
                It.Is<UpdateAuthorityDto>(dto =>
                    dto.Name == updateDto.Name &&
                    dto.Description == updateDto.Description)),
                Times.Once);
        }


        [Fact]
        public async Task UpdateAuthority_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            var updateDto = new UpdateAuthorityDto
            {
                Name = "UpdatedRole",
                Description = "Updated description"
            };
            
            _mockAuthorizationManager
                .Setup(x => x.UpdateAuthorityAsync(authorityId, It.IsAny<UpdateAuthorityDto>()))
                .ThrowsAsync(new NotFoundException($"Authority with id {authorityId} not found"));

            // Act
            var result = await _controller.UpdateAuthority(authorityId, updateDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAuthority_WithDuplicateName_ReturnsConflict()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            var updateDto = new UpdateAuthorityDto
            {
                Name = "ExistingRole",
                Description = "Updated description"
            };
            
            _mockAuthorizationManager
                .Setup(x => x.UpdateAuthorityAsync(authorityId, It.IsAny<UpdateAuthorityDto>()))
                .ThrowsAsync(new InvalidOperationException("Authority with name 'ExistingRole' already exists"));

            // Act
            var result = await _controller.UpdateAuthority(authorityId, updateDto);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAuthority_WithSystemAuthority_ReturnsBadRequest()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            var updateDto = new UpdateAuthorityDto
            {
                Name = "OWNER",
                Description = "Updated description"
            };
            
            _mockAuthorizationManager
                .Setup(x => x.UpdateAuthorityAsync(authorityId, It.IsAny<UpdateAuthorityDto>()))
                .ThrowsAsync(new InvalidOperationException("Cannot modify system authority"));

            // Act
            var result = await _controller.UpdateAuthority(authorityId, updateDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region DeleteAuthority Tests

        [Fact]
        public async Task DeleteAuthority_WithValidId_ReturnsOk()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            
            _mockAuthorizationManager
                .Setup(x => x.DeleteAuthorityAsync(authorityId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteAuthority(authorityId);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.DeleteAuthorityAsync(authorityId), Times.Once);
        }

        [Fact]
        public async Task DeleteAuthority_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            
            _mockAuthorizationManager
                .Setup(x => x.DeleteAuthorityAsync(authorityId))
                .ThrowsAsync(new NotFoundException($"Authority with id {authorityId} not found"));

            // Act
            var result = await _controller.DeleteAuthority(authorityId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAuthority_WithSystemAuthority_ReturnsConflict()
        {
            // Arrange
            var authorityId = Guid.NewGuid();
            
            _mockAuthorizationManager
                .Setup(x => x.DeleteAuthorityAsync(authorityId))
                .ThrowsAsync(new InvalidOperationException("Cannot delete system authority"));

            // Act
            var result = await _controller.DeleteAuthority(authorityId);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        #endregion

        #region GetUserAuthorities Tests

        [Fact]
        public async Task GetUserAuthorities_ReturnsOkWithAuthorities()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorities = new List<Authority>
            {
                new Authority { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrator role" },
                new Authority { Id = Guid.NewGuid(), Name = "User", Description = "Regular user role" }
            };
            
            _mockAuthorizationManager
                .Setup(x => x.GetUserAuthoritiesAsync(userId))
                .ReturnsAsync(authorities);

            // Act
            var result = await _controller.GetUserAuthorities(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAuthorities = Assert.IsAssignableFrom<IEnumerable<AuthorityDto>>(okResult.Value);
            Assert.Equal(2, returnedAuthorities.Count());
            Assert.Contains(returnedAuthorities, a => a.Name == "Admin");
            Assert.Contains(returnedAuthorities, a => a.Name == "User");
        }

        #endregion

        #region AssignAuthorityToUser Tests

        [Fact]
        public async Task AssignAuthorityToUser_WithValidData_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();
            var assignDto = new AssignAuthorityDto { AuthorityId = authorityId };
            
            _mockAuthorizationManager
                .Setup(x => x.AssignAuthorityToUserAsync(userId, authorityId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AssignAuthorityToUser(userId, assignDto);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.AssignAuthorityToUserAsync(userId, authorityId), Times.Once);
        }

        #endregion

        #region RemoveAuthorityFromUser Tests

        [Fact]
        public async Task RemoveAuthorityFromUser_WithValidData_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();
            
            _mockAuthorizationManager
                .Setup(x => x.RemoveAuthorityFromUserAsync(userId, authorityId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveAuthorityFromUser(userId, authorityId);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.RemoveAuthorityFromUserAsync(userId, authorityId), Times.Once);
        }

        #endregion

        #region GetAvailableResources Tests

        [Fact]
        public async Task GetAvailableResources_ReturnsOkWithResources()
        {
            // Arrange
            var resources = new List<AvailableResourceDto>
            {
                new AvailableResourceDto
                {
                    ResourceName = Resources.OrganizationUnitResource,
                    DisplayName = "Organization Unit",
                    AvailablePermissions = new List<PermissionLevelDto>
                    {
                        new PermissionLevelDto { Level = 4, Name = "Admin", Description = "Full access" }
                    }
                },
                new AvailableResourceDto
                {
                    ResourceName = Resources.UserResource,
                    DisplayName = "User",
                    AvailablePermissions = new List<PermissionLevelDto>
                    {
                        new PermissionLevelDto { Level = 3, Name = "Update", Description = "Can update" }
                    }
                }
            };
            
            _mockAuthorizationManager
                .Setup(x => x.GetAvailableResourcesAsync())
                .ReturnsAsync(resources);

            // Act
            var result = await _controller.GetAvailableResources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<AvailableResourceDto>>(okResult.Value);
            Assert.Equal(2, returnedResources.Count());
            Assert.Contains(returnedResources, r => r.ResourceName == Resources.OrganizationUnitResource);
            Assert.Contains(returnedResources, r => r.ResourceName == Resources.UserResource);
        }

        #endregion

        #region AddResourcePermission Tests

        [Fact]
        public async Task AddResourcePermission_WithValidData_ReturnsOk()
        {
            // Arrange
            var permissionDto = new ResourcePermissionDto
            {
                AuthorityName = "TestRole",
                ResourceName = Resources.AssetResource,
                Permission = 2 // Create permission
            };
            
            _mockAuthorizationManager
                .Setup(x => x.AddResourcePermissionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddResourcePermission(permissionDto);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.AddResourcePermissionAsync(
                permissionDto.AuthorityName,
                permissionDto.ResourceName,
                permissionDto.Permission), 
                Times.Once);
        }

        #endregion

        #region RemoveResourcePermission Tests

        [Fact]
        public async Task RemoveResourcePermission_WithValidData_ReturnsOk()
        {
            // Arrange
            var authorityName = "TestRole";
            var resourceName = Resources.AssetResource;
            
            _mockAuthorizationManager
                .Setup(x => x.RemoveResourcePermissionAsync(authorityName, resourceName))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveResourcePermission(authorityName, resourceName);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.RemoveResourcePermissionAsync(authorityName, resourceName), Times.Once);
        }

        #endregion

        #region AssignAuthoritiesToUserBulk Tests

        [Fact]
        public async Task AssignAuthoritiesToUserBulk_WithValidData_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenant = "test-tenant";
            var assignDto = new AssignAuthoritiesDto
            {
                AuthorityIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };
            
            var organizationUnit = new OrganizationUnitResponseDto
            {
                Id = _tenantId,
                Name = "Test Organization",
                Slug = tenant
            };
            
            _mockOrganizationUnitService
                .Setup(x => x.GetOrganizationUnitBySlugAsync(It.IsAny<string>()))
                .ReturnsAsync(organizationUnit);
                
            _mockAuthorizationManager
                .Setup(x => x.AssignAuthoritiesToUserAsync(userId, It.IsAny<List<Guid>>(), _tenantId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AssignAuthoritiesToUserBulk(tenant, userId, assignDto);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockAuthorizationManager.Verify(x => x.AssignAuthoritiesToUserAsync(
                userId, 
                It.Is<List<Guid>>(ids => ids.Count == assignDto.AuthorityIds.Count),
                _tenantId), 
                Times.Once);
        }

        #endregion
    }
} 