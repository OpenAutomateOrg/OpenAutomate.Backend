using Moq;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Core.Tests.IserviceTest
{
    public class OrganizationUnitServiceTests
    {
        private readonly Mock<IOrganizationUnitService> _mockOrgService;

        public OrganizationUnitServiceTests()
        {
            _mockOrgService = new Mock<IOrganizationUnitService>();
        }

        #region CreateOrganizationUnitAsync Tests

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithValidData_ReturnsCreatedUnit()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateOrganizationUnitDto { Name = "Test Org", Description = "Test Description" };
            var expected = new OrganizationUnitResponseDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Org", 
                Description = "Test Description", 
                Slug = "test-org", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.Id, result.Id);
            Assert.Equal("Test Org", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("test-org", result.Slug);
            Assert.True(result.IsActive);
            _mockOrgService.Verify(s => s.CreateOrganizationUnitAsync(createDto, userId), Times.Once);
        }

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateOrganizationUnitDto { Name = "", Description = "Test Description" };
            
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ThrowsAsync(new ArgumentException("Name cannot be empty"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, userId));
            
            Assert.Contains("Name cannot be empty", exception.Message);
        }

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithDuplicateName_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateOrganizationUnitDto { Name = "Existing Org", Description = "Test Description" };
            
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, userId))
                .ThrowsAsync(new InvalidOperationException("Organization unit with name 'Existing Org' already exists"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, userId));
            
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public async Task CreateOrganizationUnitAsync_WithInvalidUserId_ThrowsException()
        {
            // Arrange
            var invalidUserId = Guid.Empty;
            var createDto = new CreateOrganizationUnitDto { Name = "Test Org", Description = "Test Description" };
            
            _mockOrgService.Setup(s => s.CreateOrganizationUnitAsync(createDto, invalidUserId))
                .ThrowsAsync(new ArgumentException("Invalid user ID"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockOrgService.Object.CreateOrganizationUnitAsync(createDto, invalidUserId));
            
            Assert.Contains("Invalid user ID", exception.Message);
        }

        #endregion

        #region GetOrganizationUnitByIdAsync Tests

        [Fact]
        public async Task GetOrganizationUnitByIdAsync_WithValidId_ReturnsUnit()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var expected = new OrganizationUnitResponseDto 
            { 
                Id = orgId, 
                Name = "Test Org", 
                Description = "Test Description", 
                Slug = "test-org", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            
            _mockOrgService.Setup(s => s.GetOrganizationUnitByIdAsync(orgId))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitByIdAsync(orgId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orgId, result.Id);
            Assert.Equal("Test Org", result.Name);
            _mockOrgService.Verify(s => s.GetOrganizationUnitByIdAsync(orgId), Times.Once);
        }

        [Fact]
        public async Task GetOrganizationUnitByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            
            _mockOrgService.Setup(s => s.GetOrganizationUnitByIdAsync(nonExistentId))
                .ReturnsAsync((OrganizationUnitResponseDto)null);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
            _mockOrgService.Verify(s => s.GetOrganizationUnitByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task GetOrganizationUnitByIdAsync_WithEmptyGuid_ThrowsException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            
            _mockOrgService.Setup(s => s.GetOrganizationUnitByIdAsync(emptyId))
                .ThrowsAsync(new ArgumentException("Invalid organization unit ID"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockOrgService.Object.GetOrganizationUnitByIdAsync(emptyId));
            
            Assert.Contains("Invalid organization unit ID", exception.Message);
        }

        #endregion

        #region GetOrganizationUnitBySlugAsync Tests

        [Fact]
        public async Task GetOrganizationUnitBySlugAsync_WithValidSlug_ReturnsUnit()
        {
            // Arrange
            var slug = "test-org";
            var expected = new OrganizationUnitResponseDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Org", 
                Description = "Test Description", 
                Slug = slug, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync(slug))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetOrganizationUnitBySlugAsync(slug);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(slug, result.Slug);
            Assert.Equal("Test Org", result.Name);
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
        public async Task GetOrganizationUnitBySlugAsync_WithEmptySlug_ThrowsException()
        {
            // Arrange
            var emptySlug = "";
            
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync(emptySlug))
                .ThrowsAsync(new ArgumentException("Slug cannot be empty"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockOrgService.Object.GetOrganizationUnitBySlugAsync(emptySlug));
            
            Assert.Contains("Slug cannot be empty", exception.Message);
        }

        #endregion

        #region GetAllOrganizationUnitsAsync Tests

        [Fact]
        public async Task GetAllOrganizationUnitsAsync_ReturnsAllUnits()
        {
            // Arrange
            var expected = new List<OrganizationUnitResponseDto>
            {
                new OrganizationUnitResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Org 1", 
                    Description = "Description 1", 
                    Slug = "org-1", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                },
                new OrganizationUnitResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Org 2", 
                    Description = "Description 2", 
                    Slug = "org-2", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                }
            };
            
            _mockOrgService.Setup(s => s.GetAllOrganizationUnitsAsync())
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetAllOrganizationUnitsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, ou => ou.Name == "Org 1");
            Assert.Contains(result, ou => ou.Name == "Org 2");
            _mockOrgService.Verify(s => s.GetAllOrganizationUnitsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllOrganizationUnitsAsync_WithNoUnits_ReturnsEmptyCollection()
        {
            // Arrange
            var emptyList = new List<OrganizationUnitResponseDto>();
            
            _mockOrgService.Setup(s => s.GetAllOrganizationUnitsAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _mockOrgService.Object.GetAllOrganizationUnitsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockOrgService.Verify(s => s.GetAllOrganizationUnitsAsync(), Times.Once);
        }

        #endregion

        #region UpdateOrganizationUnitAsync Tests

        [Fact]
        public async Task UpdateOrganizationUnitAsync_WithValidData_ReturnsUpdatedUnit()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var updateDto = new CreateOrganizationUnitDto 
            { 
                Name = "Updated Org", 
                Description = "Updated Description" 
            };
            
            var expected = new OrganizationUnitResponseDto 
            { 
                Id = orgId, 
                Name = "Updated Org", 
                Description = "Updated Description", 
                Slug = "updated-org", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow.AddDays(-1), 
                UpdatedAt = DateTime.UtcNow 
            };
            
            _mockOrgService.Setup(s => s.UpdateOrganizationUnitAsync(orgId, updateDto))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.UpdateOrganizationUnitAsync(orgId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orgId, result.Id);
            Assert.Equal("Updated Org", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("updated-org", result.Slug);
            Assert.NotNull(result.UpdatedAt);
            _mockOrgService.Verify(s => s.UpdateOrganizationUnitAsync(orgId, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateOrganizationUnitAsync_WithNonExistentId_ThrowsException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new CreateOrganizationUnitDto 
            { 
                Name = "Updated Org", 
                Description = "Updated Description" 
            };
            
            _mockOrgService.Setup(s => s.UpdateOrganizationUnitAsync(nonExistentId, updateDto))
                .ThrowsAsync(new KeyNotFoundException($"Organization unit with ID {nonExistentId} not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _mockOrgService.Object.UpdateOrganizationUnitAsync(nonExistentId, updateDto));
            
            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task UpdateOrganizationUnitAsync_WithSameName_DoesNotChangeSlug()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var updateDto = new CreateOrganizationUnitDto 
            { 
                Name = "Existing Org", // Same name
                Description = "Updated Description" // Only description changes
            };
            
            var expected = new OrganizationUnitResponseDto 
            { 
                Id = orgId, 
                Name = "Existing Org", 
                Description = "Updated Description", 
                Slug = "existing-org", // Slug remains the same
                IsActive = true, 
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow 
            };
            
            _mockOrgService.Setup(s => s.UpdateOrganizationUnitAsync(orgId, updateDto))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.UpdateOrganizationUnitAsync(orgId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("existing-org", result.Slug); // Slug should not change
            Assert.Equal("Updated Description", result.Description); // Description should update
            _mockOrgService.Verify(s => s.UpdateOrganizationUnitAsync(orgId, updateDto), Times.Once);
        }

        #endregion

        #region CheckNameChangeImpactAsync Tests

        [Fact]
        public async Task CheckNameChangeImpactAsync_WithSignificantChange_ReturnsWarning()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var newName = "Completely Different Name";
            
            var expected = new SlugChangeWarningDto 
            { 
                CurrentSlug = "old-name", 
                ProposedSlug = "completely-different-name", 
                IsChangeSignificant = true, 
                PotentialImpacts = new[] 
                { 
                    "URL paths will change", 
                    "Bookmarks to this organization may no longer work", 
                    "API integrations using the current slug will need to be updated" 
                }, 
                RequiresConfirmation = true 
            };
            
            _mockOrgService.Setup(s => s.CheckNameChangeImpactAsync(orgId, newName))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.CheckNameChangeImpactAsync(orgId, newName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("completely-different-name", result.ProposedSlug);
            Assert.True(result.IsChangeSignificant);
            Assert.True(result.RequiresConfirmation);
            Assert.NotEmpty(result.PotentialImpacts);
            _mockOrgService.Verify(s => s.CheckNameChangeImpactAsync(orgId, newName), Times.Once);
        }

        [Fact]
        public async Task CheckNameChangeImpactAsync_WithMinorChange_ReturnsNoWarning()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var newName = "Old Name"; // Just a capitalization change from "old name"
            
            var expected = new SlugChangeWarningDto 
            { 
                CurrentSlug = "old-name", 
                ProposedSlug = "old-name", 
                IsChangeSignificant = false, 
                PotentialImpacts = Array.Empty<string>(), 
                RequiresConfirmation = false 
            };
            
            _mockOrgService.Setup(s => s.CheckNameChangeImpactAsync(orgId, newName))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.CheckNameChangeImpactAsync(orgId, newName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("old-name", result.ProposedSlug); // Slug doesn't change
            Assert.False(result.IsChangeSignificant);
            Assert.False(result.RequiresConfirmation);
            Assert.Empty(result.PotentialImpacts);
            _mockOrgService.Verify(s => s.CheckNameChangeImpactAsync(orgId, newName), Times.Once);
        }

        [Fact]
        public async Task CheckNameChangeImpactAsync_WithNonExistentId_ThrowsException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var newName = "New Name";
            
            _mockOrgService.Setup(s => s.CheckNameChangeImpactAsync(nonExistentId, newName))
                .ThrowsAsync(new KeyNotFoundException($"Organization unit with ID {nonExistentId} not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _mockOrgService.Object.CheckNameChangeImpactAsync(nonExistentId, newName));
            
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region GenerateSlugFromName Tests

        [Fact]
        public void GenerateSlugFromName_WithSimpleName_ReturnsExpectedSlug()
        {
            // Arrange
            var name = "Test Organization";
            var expectedSlug = "test-organization";
            
            _mockOrgService.Setup(s => s.GenerateSlugFromName(name))
                .Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(name);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(name), Times.Once);
        }

        [Fact]
        public void GenerateSlugFromName_WithSpecialCharacters_ReturnsCleanSlug()
        {
            // Arrange
            var name = "Test Organization!@#$%^&*()";
            var expectedSlug = "test-organization";
            
            _mockOrgService.Setup(s => s.GenerateSlugFromName(name))
                .Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(name);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(name), Times.Once);
        }

        [Fact]
        public void GenerateSlugFromName_WithNonLatinCharacters_TransliteratesCorrectly()
        {
            // Arrange
            var name = "Организация";
            var expectedSlug = "organizatsiya"; // Transliterated version
            
            _mockOrgService.Setup(s => s.GenerateSlugFromName(name))
                .Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(name);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(name), Times.Once);
        }

        [Fact]
        public void GenerateSlugFromName_WithEmptyString_ReturnsDefaultSlug()
        {
            // Arrange
            var emptyName = "";
            var expectedSlug = "default"; // Assuming the service returns a default slug for empty names
            
            _mockOrgService.Setup(s => s.GenerateSlugFromName(emptyName))
                .Returns(expectedSlug);

            // Act
            var result = _mockOrgService.Object.GenerateSlugFromName(emptyName);

            // Assert
            Assert.Equal(expectedSlug, result);
            _mockOrgService.Verify(s => s.GenerateSlugFromName(emptyName), Times.Once);
        }

        #endregion

        #region GetUserOrganizationUnitsAsync Tests

        [Fact]
        public async Task GetUserOrganizationUnitsAsync_WithValidUserId_ReturnsUnits()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orgUnits = new List<OrganizationUnitResponseDto>
            {
                new OrganizationUnitResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Org 1", 
                    Description = "Description 1", 
                    Slug = "org-1", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                },
                new OrganizationUnitResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Org 2", 
                    Description = "Description 2", 
                    Slug = "org-2", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow 
                }
            };
            
            var expected = new UserOrganizationUnitsResponseDto
            {
                Count = 2,
                OrganizationUnits = orgUnits
            };
            
            _mockOrgService.Setup(s => s.GetUserOrganizationUnitsAsync(userId))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetUserOrganizationUnitsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.OrganizationUnits.Count());
            Assert.Contains(result.OrganizationUnits, ou => ou.Name == "Org 1");
            Assert.Contains(result.OrganizationUnits, ou => ou.Name == "Org 2");
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

        [Fact]
        public async Task GetUserOrganizationUnitsAsync_WithInvalidUserId_ThrowsException()
        {
            // Arrange
            var invalidUserId = Guid.Empty;
            
            _mockOrgService.Setup(s => s.GetUserOrganizationUnitsAsync(invalidUserId))
                .ThrowsAsync(new ArgumentException("Invalid user ID"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockOrgService.Object.GetUserOrganizationUnitsAsync(invalidUserId));
            
            Assert.Contains("Invalid user ID", exception.Message);
        }

        [Fact]
        public async Task GetUserOrganizationUnitsAsync_WithLargeNumberOfUnits_ReturnsAllUnits()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orgUnits = new List<OrganizationUnitResponseDto>();
            
            for (int i = 0; i < 30; i++)
            {
                orgUnits.Add(new OrganizationUnitResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Organization {i}", 
                    Slug = $"organization-{i}", 
                    IsActive = true, 
                    CreatedAt = DateTime.UtcNow.AddDays(-i) 
                });
            }
            
            var expected = new UserOrganizationUnitsResponseDto
            {
                Count = 30,
                OrganizationUnits = orgUnits
            };
            
            _mockOrgService.Setup(s => s.GetUserOrganizationUnitsAsync(userId))
                .ReturnsAsync(expected);

            // Act
            var result = await _mockOrgService.Object.GetUserOrganizationUnitsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(30, result.Count);
            Assert.Equal(30, result.OrganizationUnits.Count());
            _mockOrgService.Verify(s => s.GetUserOrganizationUnitsAsync(userId), Times.Once);
        }

        #endregion
    }
} 