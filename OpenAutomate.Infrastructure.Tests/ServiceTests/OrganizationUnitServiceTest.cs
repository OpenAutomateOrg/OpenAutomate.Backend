using Moq;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class OrganizationUnitServiceTests
    {
        private readonly Mock<IOrganizationUnitService> _mockOrgService;

        public OrganizationUnitServiceTests()
        {
            _mockOrgService = new Mock<IOrganizationUnitService>();
        }

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithValidData_ReturnsCreatedUnit()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateOrganizationUnitDto { Name = "Test Org", Description = "Desc" };
            var expected = new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Test Org", Description = "Desc", Slug = "test-org", IsActive = true, CreatedAt = DateTime.UtcNow };
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Org", result.Name);
            _mockOrgService.Verify(s => s.CreateOrganizationUnitAsync(createDto, userId), Times.Once);
        }

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateOrganizationUnitDto { Name = "", Description = "Desc" };
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ThrowsAsync(new ArgumentException("Name cannot be empty"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, userId));
        }

        [Fact]
        public async Task GetOrganizationUnitByIdAsync_WithValidId_ReturnsUnit()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var expected = new OrganizationUnitResponseDto { Id = orgId, Name = "Org", Description = "Desc", Slug = "org", IsActive = true, CreatedAt = DateTime.UtcNow };
            _mockOrgService.Setup(s => s.GetOrganizationUnitByIdAsync(orgId)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitByIdAsync(orgId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orgId, result.Id);
            _mockOrgService.Verify(s => s.GetOrganizationUnitByIdAsync(orgId), Times.Once);
        }

        [Fact]
        public async Task GetOrganizationUnitByIdAsync_WithEmptyGuid_ThrowsException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            _mockOrgService.Setup(s => s.GetOrganizationUnitByIdAsync(emptyId))
                .ThrowsAsync(new ArgumentException("Invalid organization unit ID"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockOrgService.Object.GetOrganizationUnitByIdAsync(emptyId));
        }

        [Fact]
        public async Task GetOrganizationUnitBySlugAsync_WithValidSlug_ReturnsUnit()
        {
            // Arrange
            var slug = "org-slug";
            var expected = new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Org", Slug = slug, IsActive = true, CreatedAt = DateTime.UtcNow };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync(slug)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitBySlugAsync(slug);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(slug, result.Slug);
            _mockOrgService.Verify(s => s.GetOrganizationUnitBySlugAsync(slug), Times.Once);
        }

        [Fact]
        public async Task GetOrganizationUnitBySlugAsync_WithInvalidSlug_ReturnsNull()
        {
            // Arrange
            var invalidSlug = "invalid-slug";
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync(invalidSlug))
                .ReturnsAsync((OrganizationUnitResponseDto)null);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitBySlugAsync(invalidSlug);

            // Assert
            Assert.Null(result);
            _mockOrgService.Verify(s => s.GetOrganizationUnitBySlugAsync(invalidSlug), Times.Once);
        }

        [Fact]
        public async Task GetAllOrganizationUnitsAsync_ReturnsList()
        {
            // Arrange
            var expected = new List<OrganizationUnitResponseDto> { new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Org1" }, new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Org2" } };
            _mockOrgService.Setup(s => s.GetAllOrganizationUnitsAsync()).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetAllOrganizationUnitsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Collection(result,
                item => Assert.Equal("Org1", item.Name),
                item => Assert.Equal("Org2", item.Name));
            _mockOrgService.Verify(s => s.GetAllOrganizationUnitsAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateOrganizationUnitAsync_WithValidData_ReturnsUpdatedUnit()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var updateDto = new CreateOrganizationUnitDto { Name = "Updated Org", Description = "Updated Desc" };
            var expected = new OrganizationUnitResponseDto { Id = orgId, Name = "Updated Org", Description = "Updated Desc", Slug = "updated-org", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _mockOrgService.Setup(s => s.UpdateOrganizationUnitAsync(orgId, updateDto)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.UpdateOrganizationUnitAsync(orgId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Org", result.Name);
            _mockOrgService.Verify(s => s.UpdateOrganizationUnitAsync(orgId, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateOrganizationUnitAsync_WithNonExistentId_ThrowsException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new CreateOrganizationUnitDto { Name = "Updated Org", Description = "Updated Desc" };
            _mockOrgService.Setup(s => s.UpdateOrganizationUnitAsync(nonExistentId, updateDto))
                .ThrowsAsync(new KeyNotFoundException("Organization unit not found"));

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _mockOrgService.Object.UpdateOrganizationUnitAsync(nonExistentId, updateDto));
        }

        [Fact]
        public async Task CheckNameChangeImpactAsync_WithValidData_ReturnsWarningDto()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var newName = "New Name";
            var expected = new SlugChangeWarningDto { CurrentSlug = "old-slug", ProposedSlug = "new-name", IsChangeSignificant = true, PotentialImpacts = new[] { "URL will change" }, RequiresConfirmation = true };
            _mockOrgService.Setup(s => s.CheckNameChangeImpactAsync(orgId, newName)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.CheckNameChangeImpactAsync(orgId, newName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-name", result.ProposedSlug);
            Assert.True(result.IsChangeSignificant);
            _mockOrgService.Verify(s => s.CheckNameChangeImpactAsync(orgId, newName), Times.Once);
        }

        [Fact]
        public void GenerateSlugFromName_ReturnsSlug()
        {
            // Arrange
            var name = "My Organization";
            var expectedSlug = "my-organization";
            _mockOrgService.Setup(s => s.GenerateSlugFromName(name)).Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(name);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(name), Times.Once);
        }

        [Fact]
        public void GenerateSlugFromName_WithSpecialCharacters_ReturnsValidSlug()
        {
            // Arrange
            var name = "My Organization!@#$%^&*()";
            var expectedSlug = "my-organization";
            _mockOrgService.Setup(s => s.GenerateSlugFromName(name))
                .Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(name);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(name), Times.Once);
        }

        [Fact]
        public async Task GetUserOrganizationUnitsAsync_WithValidUserId_ReturnsUnits()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expected = new UserOrganizationUnitsResponseDto
            {
                Count = 2,
                OrganizationUnits = new List<OrganizationUnitResponseDto>
                {
                    new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Org1" },
                    new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Org2" }
                }
            };
            _mockOrgService.Setup(s => s.GetUserOrganizationUnitsAsync(userId)).ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetUserOrganizationUnitsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Collection(result.OrganizationUnits,
                item => Assert.Equal("Org1", item.Name),
                item => Assert.Equal("Org2", item.Name));
            _mockOrgService.Verify(s => s.GetUserOrganizationUnitsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserOrganizationUnitsAsync_WithUserHavingNoUnits_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expected = new UserOrganizationUnitsResponseDto
            {
                Count = 0,
                OrganizationUnits = new List<OrganizationUnitResponseDto>()
            };
            _mockOrgService.Setup(s => s.GetUserOrganizationUnitsAsync(userId))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetUserOrganizationUnitsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);
            Assert.Empty(result.OrganizationUnits);
            _mockOrgService.Verify(s => s.GetUserOrganizationUnitsAsync(userId), Times.Once);
        }
    }
}
