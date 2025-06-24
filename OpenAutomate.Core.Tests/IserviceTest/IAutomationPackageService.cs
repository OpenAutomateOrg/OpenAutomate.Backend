using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;
using Xunit;

namespace OpenAutomate.Core.Tests.IserviceTest
{
    public class AutomationPackageServiceTests
    {
        private readonly Mock<IAutomationPackageService> _mockPackageService;

        public AutomationPackageServiceTests()
        {
            _mockPackageService = new Mock<IAutomationPackageService>();
        }

        [Fact]
        public async Task CreatePackageAsync_WithValidData_ReturnsCreatedPackage()
        {
            // Arrange
            var createDto = new CreateAutomationPackageDto
            {
                Name = "TestPackage",
                Description = "Test package description"
            };

            var expectedPackage = new AutomationPackageResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "TestPackage",
                Description = "Test package description",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.CreatePackageAsync(createDto))
                .ReturnsAsync(expectedPackage);

            // Act
            var result = await _mockPackageService.Object.CreatePackageAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPackage.Id, result.Id);
            Assert.Equal("TestPackage", result.Name);
            Assert.Equal("Test package description", result.Description);
            Assert.True(result.IsActive);
            _mockPackageService.Verify(s => s.CreatePackageAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreatePackageAsync_WithDuplicateName_ThrowsException()
        {
            // Arrange
            var createDto = new CreateAutomationPackageDto
            {
                Name = "ExistingPackage",
                Description = "Description"
            };

            _mockPackageService.Setup(s => s.CreatePackageAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Package with name 'ExistingPackage' already exists"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockPackageService.Object.CreatePackageAsync(createDto));
            
            Assert.Contains("already exists", exception.Message);
            _mockPackageService.Verify(s => s.CreatePackageAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task GetPackageByIdAsync_WithValidId_ReturnsPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var expectedPackage = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage",
                Description = "Description",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Versions = new List<PackageVersionResponseDto>()
            };

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(expectedPackage);

            // Act
            var result = await _mockPackageService.Object.GetPackageByIdAsync(packageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(packageId, result.Id);
            Assert.Equal("TestPackage", result.Name);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
        }

        [Fact]
        public async Task GetPackageByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.GetPackageByIdAsync(invalidId))
                .ReturnsAsync((AutomationPackageResponseDto)null);

            // Act
            var result = await _mockPackageService.Object.GetPackageByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task GetPackageByNameAsync_WithValidName_ReturnsPackage()
        {
            // Arrange
            var packageName = "TestPackage";
            var expectedPackage = new AutomationPackageResponseDto
            {
                Id = Guid.NewGuid(),
                Name = packageName,
                Description = "Description",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Versions = new List<PackageVersionResponseDto>()
            };

            _mockPackageService.Setup(s => s.GetPackageByNameAsync(packageName))
                .ReturnsAsync(expectedPackage);

            // Act
            var result = await _mockPackageService.Object.GetPackageByNameAsync(packageName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(packageName, result.Name);
            _mockPackageService.Verify(s => s.GetPackageByNameAsync(packageName), Times.Once);
        }

        [Fact]
        public async Task GetPackageByNameAsync_WithInvalidName_ReturnsNull()
        {
            // Arrange
            var invalidName = "NonExistentPackage";
            _mockPackageService.Setup(s => s.GetPackageByNameAsync(invalidName))
                .ReturnsAsync((AutomationPackageResponseDto)null);

            // Act
            var result = await _mockPackageService.Object.GetPackageByNameAsync(invalidName);

            // Assert
            Assert.Null(result);
            _mockPackageService.Verify(s => s.GetPackageByNameAsync(invalidName), Times.Once);
        }

        [Fact]
        public async Task GetAllPackagesAsync_ReturnsAllPackages()
        {
            // Arrange
            var expectedPackages = new List<AutomationPackageResponseDto>
            {
                new AutomationPackageResponseDto { Id = Guid.NewGuid(), Name = "Package1", Description = "Description1", IsActive = true, CreatedAt = DateTime.UtcNow },
                new AutomationPackageResponseDto { Id = Guid.NewGuid(), Name = "Package2", Description = "Description2", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            _mockPackageService.Setup(s => s.GetAllPackagesAsync())
                .ReturnsAsync(expectedPackages);

            // Act
            var result = await _mockPackageService.Object.GetAllPackagesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Name == "Package1");
            Assert.Contains(result, p => p.Name == "Package2");
            _mockPackageService.Verify(s => s.GetAllPackagesAsync(), Times.Once);
        }

        [Fact]
        public async Task UploadPackageVersionAsync_WithValidData_ReturnsVersionInfo()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var versionNumber = "1.0.0";
            var fileName = "package.zip";
            var fileStream = new MemoryStream();
            
            var expectedVersion = new PackageVersionResponseDto
            {
                Id = Guid.NewGuid(),
                VersionNumber = versionNumber,
                FileName = fileName,
                FileSize = 1024,
                ContentType = "application/zip",
                IsActive = true,
                UploadedAt = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.UploadPackageVersionAsync(packageId, fileStream, fileName, versionNumber))
                .ReturnsAsync(expectedVersion);

            // Act
            var result = await _mockPackageService.Object.UploadPackageVersionAsync(packageId, fileStream, fileName, versionNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(versionNumber, result.VersionNumber);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal("application/zip", result.ContentType);
            _mockPackageService.Verify(s => s.UploadPackageVersionAsync(packageId, fileStream, fileName, versionNumber), Times.Once);
        }

        [Fact]
        public async Task UploadPackageVersionAsync_WithInvalidPackageId_ThrowsException()
        {
            // Arrange
            var invalidPackageId = Guid.NewGuid();
            var versionNumber = "1.0.0";
            var fileName = "package.zip";
            var fileStream = new MemoryStream();

            _mockPackageService.Setup(s => s.UploadPackageVersionAsync(invalidPackageId, fileStream, fileName, versionNumber))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.UploadPackageVersionAsync(invalidPackageId, fileStream, fileName, versionNumber));
            
            Assert.Equal("Package not found", exception.Message);
            _mockPackageService.Verify(s => s.UploadPackageVersionAsync(invalidPackageId, fileStream, fileName, versionNumber), Times.Once);
        }

        [Fact]
        public async Task UploadPackageVersionAsync_WithDuplicateVersion_ThrowsException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var existingVersion = "1.0.0";
            var fileName = "package.zip";
            var fileStream = new MemoryStream();

            _mockPackageService.Setup(s => s.UploadPackageVersionAsync(packageId, fileStream, fileName, existingVersion))
                .ThrowsAsync(new InvalidOperationException($"Version {existingVersion} already exists for this package"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockPackageService.Object.UploadPackageVersionAsync(packageId, fileStream, fileName, existingVersion));
            
            Assert.Contains("already exists", exception.Message);
            _mockPackageService.Verify(s => s.UploadPackageVersionAsync(packageId, fileStream, fileName, existingVersion), Times.Once);
        }

        [Fact]
        public async Task GetPackageDownloadUrlAsync_WithValidData_ReturnsUrl()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var versionNumber = "1.0.0";
            var expectedUrl = "https://storage.example.com/packages/package-1.0.0.zip?signature=abc123";

            _mockPackageService.Setup(s => s.GetPackageDownloadUrlAsync(packageId, versionNumber))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _mockPackageService.Object.GetPackageDownloadUrlAsync(packageId, versionNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUrl, result);
            _mockPackageService.Verify(s => s.GetPackageDownloadUrlAsync(packageId, versionNumber), Times.Once);
        }

        [Fact]
        public async Task GetPackageDownloadUrlAsync_WithInvalidPackageId_ThrowsException()
        {
            // Arrange
            var invalidPackageId = Guid.NewGuid();
            var versionNumber = "1.0.0";

            _mockPackageService.Setup(s => s.GetPackageDownloadUrlAsync(invalidPackageId, versionNumber))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.GetPackageDownloadUrlAsync(invalidPackageId, versionNumber));
            
            Assert.Equal("Package not found", exception.Message);
            _mockPackageService.Verify(s => s.GetPackageDownloadUrlAsync(invalidPackageId, versionNumber), Times.Once);
        }

        [Fact]
        public async Task GetPackageDownloadUrlAsync_WithInvalidVersion_ThrowsException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var invalidVersion = "9.9.9";

            _mockPackageService.Setup(s => s.GetPackageDownloadUrlAsync(packageId, invalidVersion))
                .ThrowsAsync(new ArgumentException($"Version {invalidVersion} not found for this package"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.GetPackageDownloadUrlAsync(packageId, invalidVersion));
            
            Assert.Contains("not found", exception.Message);
            _mockPackageService.Verify(s => s.GetPackageDownloadUrlAsync(packageId, invalidVersion), Times.Once);
        }

        [Fact]
        public async Task DeletePackageAsync_WithValidId_DeletesPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .Returns(Task.CompletedTask);

            // Act
            await _mockPackageService.Object.DeletePackageAsync(packageId);

            // Assert
            _mockPackageService.Verify(s => s.DeletePackageAsync(packageId), Times.Once);
        }

        [Fact]
        public async Task DeletePackageAsync_WithInvalidId_ThrowsException()
        {
            // Arrange
            var invalidPackageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.DeletePackageAsync(invalidPackageId))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.DeletePackageAsync(invalidPackageId));
            
            Assert.Equal("Package not found", exception.Message);
            _mockPackageService.Verify(s => s.DeletePackageAsync(invalidPackageId), Times.Once);
        }

        [Fact]
        public async Task DeletePackageVersionAsync_WithValidData_DeletesVersion()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var versionNumber = "1.0.0";
            _mockPackageService.Setup(s => s.DeletePackageVersionAsync(packageId, versionNumber))
                .Returns(Task.CompletedTask);

            // Act
            await _mockPackageService.Object.DeletePackageVersionAsync(packageId, versionNumber);

            // Assert
            _mockPackageService.Verify(s => s.DeletePackageVersionAsync(packageId, versionNumber), Times.Once);
        }

        [Fact]
        public async Task DeletePackageVersionAsync_WithInvalidPackageId_ThrowsException()
        {
            // Arrange
            var invalidPackageId = Guid.NewGuid();
            var versionNumber = "1.0.0";
            _mockPackageService.Setup(s => s.DeletePackageVersionAsync(invalidPackageId, versionNumber))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.DeletePackageVersionAsync(invalidPackageId, versionNumber));
            
            Assert.Equal("Package not found", exception.Message);
            _mockPackageService.Verify(s => s.DeletePackageVersionAsync(invalidPackageId, versionNumber), Times.Once);
        }

        [Fact]
        public async Task DeletePackageVersionAsync_WithInvalidVersion_ThrowsException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var invalidVersion = "9.9.9";
            _mockPackageService.Setup(s => s.DeletePackageVersionAsync(packageId, invalidVersion))
                .ThrowsAsync(new ArgumentException($"Version {invalidVersion} not found for this package"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mockPackageService.Object.DeletePackageVersionAsync(packageId, invalidVersion));
            
            Assert.Contains("not found", exception.Message);
            _mockPackageService.Verify(s => s.DeletePackageVersionAsync(packageId, invalidVersion), Times.Once);
        }

        [Fact]
        public async Task PackageVersionExistsAsync_WithExistingPackageAndVersion_ReturnsTrue()
        {
            // Arrange
            var packageName = "TestPackage";
            var versionNumber = "1.0.0";
            _mockPackageService.Setup(s => s.PackageVersionExistsAsync(packageName, versionNumber))
                .ReturnsAsync(true);

            // Act
            var result = await _mockPackageService.Object.PackageVersionExistsAsync(packageName, versionNumber);

            // Assert
            Assert.True(result);
            _mockPackageService.Verify(s => s.PackageVersionExistsAsync(packageName, versionNumber), Times.Once);
        }

        [Fact]
        public async Task PackageVersionExistsAsync_WithNonExistentPackage_ReturnsFalse()
        {
            // Arrange
            var nonExistentPackage = "NonExistentPackage";
            var versionNumber = "1.0.0";
            _mockPackageService.Setup(s => s.PackageVersionExistsAsync(nonExistentPackage, versionNumber))
                .ReturnsAsync(false);

            // Act
            var result = await _mockPackageService.Object.PackageVersionExistsAsync(nonExistentPackage, versionNumber);

            // Assert
            Assert.False(result);
            _mockPackageService.Verify(s => s.PackageVersionExistsAsync(nonExistentPackage, versionNumber), Times.Once);
        }

        [Fact]
        public async Task PackageVersionExistsAsync_WithExistingPackageButNonExistentVersion_ReturnsFalse()
        {
            // Arrange
            var packageName = "TestPackage";
            var nonExistentVersion = "9.9.9";
            _mockPackageService.Setup(s => s.PackageVersionExistsAsync(packageName, nonExistentVersion))
                .ReturnsAsync(false);

            // Act
            var result = await _mockPackageService.Object.PackageVersionExistsAsync(packageName, nonExistentVersion);

            // Assert
            Assert.False(result);
            _mockPackageService.Verify(s => s.PackageVersionExistsAsync(packageName, nonExistentVersion), Times.Once);
        }
    }
}
