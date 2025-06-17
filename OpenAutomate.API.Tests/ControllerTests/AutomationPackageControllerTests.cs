using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class AutomationPackageControllerTests
    {
        private readonly Mock<IAutomationPackageService> _mockPackageService;
        private readonly Mock<IPackageMetadataService> _mockMetadataService;
        private readonly Mock<IBotAgentService> _mockBotAgentService;
        private readonly Mock<ILogger<AutomationPackageController>> _mockLogger;
        private readonly AutomationPackageController _controller;

        public AutomationPackageControllerTests()
        {
            _mockPackageService = new Mock<IAutomationPackageService>();
            _mockMetadataService = new Mock<IPackageMetadataService>();
            _mockBotAgentService = new Mock<IBotAgentService>();
            _mockLogger = new Mock<ILogger<AutomationPackageController>>();

            _controller = new AutomationPackageController(
                _mockPackageService.Object,
                _mockMetadataService.Object,
                _mockBotAgentService.Object,
                _mockLogger.Object);

            // Setup controller context
            var httpContext = new DefaultHttpContext();
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["tenant"] = "test-tenant";

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = routeData
            };
        }

        #region CreatePackage Tests

        [Fact]
        public async Task CreatePackage_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateAutomationPackageDto
            {
                Name = "TestPackage",
                Description = "Test package description"
            };

            var createdPackage = new AutomationPackageResponseDto
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _mockPackageService.Setup(x => x.CreatePackageAsync(createDto))
                .ReturnsAsync(createdPackage);

            // Act
            var result = await _controller.CreatePackage(createDto);

            // Assert
            Assert.NotNull(result.Result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
            
            var returnedPackage = Assert.IsType<AutomationPackageResponseDto>(createdAtActionResult.Value);
            Assert.Equal(createdPackage.Id, returnedPackage.Id);
            Assert.Equal(createDto.Name, returnedPackage.Name);
        }

        [Fact]
        public async Task CreatePackage_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var createDto = new CreateAutomationPackageDto
            {
                Name = "TestPackage",
                Description = "Test description"
            };

            _mockPackageService.Setup(x => x.CreatePackageAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Package already exists"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _controller.CreatePackage(createDto));
        }

        #endregion

        #region UploadPackageWithAutoCreation Tests

        [Fact]
        public async Task UploadPackageWithAutoCreation_WithValidFile_ReturnsCreatedAtAction()
        {
            // Arrange
            var fileContent = "test package content";
            var fileName = "test-package.zip";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageWithMetadataRequest
            {
                File = mockFile.Object,
                Name = "TestPackage",
                Description = "Test Description",
                Version = "1.0.0"
            };

            var metadata = new Core.IServices.PackageMetadata
            {
                IsValid = true,
                Name = "TestPackage",
                Description = "Test Description",
                Version = "1.0.0"
            };

            var createdPackage = new AutomationPackageResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "TestPackage",
                Description = "Test Description"
            };

            var packageVersion = new PackageVersionResponseDto
            {
                Id = Guid.NewGuid(),
                VersionNumber = "1.0.0"
            };

            _mockMetadataService.Setup(x => x.IsValidPackageAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(true);
            _mockMetadataService.Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(metadata);
            _mockPackageService.Setup(x => x.PackageVersionExistsAsync("TestPackage", "1.0.0"))
                .ReturnsAsync(false);
            _mockPackageService.Setup(x => x.GetPackageByNameAsync("TestPackage"))
                .ReturnsAsync((AutomationPackageResponseDto?)null);
            _mockPackageService.Setup(x => x.CreatePackageAsync(It.IsAny<CreateAutomationPackageDto>()))
                .ReturnsAsync(createdPackage);
            _mockPackageService.Setup(x => x.UploadPackageVersionAsync(
                It.IsAny<Guid>(), It.IsAny<Stream>(), fileName, "1.0.0"))
                .ReturnsAsync(packageVersion);
            _mockPackageService.Setup(x => x.GetPackageByIdAsync(createdPackage.Id))
                .ReturnsAsync(createdPackage);

            // Act
            var result = await _controller.UploadPackageWithAutoCreation(request);

            // Assert
            Assert.NotNull(result.Result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
        }

        [Fact]
        public async Task UploadPackageWithAutoCreation_WithNullFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new UploadPackageWithMetadataRequest
            {
                File = null!
            };

            // Act
            var result = await _controller.UploadPackageWithAutoCreation(request);

            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("File is required", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadPackageWithAutoCreation_WithInvalidPackage_ReturnsBadRequest()
        {
            // Arrange
            var fileContent = "invalid content";
            var fileName = "invalid.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageWithMetadataRequest
            {
                File = mockFile.Object
            };

            _mockMetadataService.Setup(x => x.IsValidPackageAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UploadPackageWithAutoCreation(request);

            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Invalid package file", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UploadPackageWithAutoCreation_WithExistingVersion_ReturnsConflict()
        {
            // Arrange
            var fileContent = "test package content";
            var fileName = "test-package.zip";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageWithMetadataRequest
            {
                File = mockFile.Object,
                Name = "TestPackage",
                Version = "1.0.0"
            };

            var metadata = new Core.IServices.PackageMetadata
            {
                IsValid = true,
                Name = "TestPackage",
                Version = "1.0.0"
            };

            _mockMetadataService.Setup(x => x.IsValidPackageAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(true);
            _mockMetadataService.Setup(x => x.ExtractMetadataAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(metadata);
            _mockPackageService.Setup(x => x.PackageVersionExistsAsync("TestPackage", "1.0.0"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UploadPackageWithAutoCreation(request);

            // Assert
            Assert.NotNull(result.Result);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.NotNull(conflictResult.Value);
        }

        #endregion

        #region GetPackageById Tests

        [Fact]
        public async Task GetPackageById_WithValidId_ReturnsOkWithPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage",
                Description = "Test Description"
            };

            _mockPackageService.Setup(x => x.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act
            var result = await _controller.GetPackageById(packageId);

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPackage = Assert.IsType<AutomationPackageResponseDto>(okResult.Value);
            Assert.Equal(packageId, returnedPackage.Id);
        }

        [Fact]
        public async Task GetPackageById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();

            _mockPackageService.Setup(x => x.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AutomationPackageResponseDto?)null);

            // Act
            var result = await _controller.GetPackageById(packageId);

            // Assert
            Assert.NotNull(result.Result);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region GetAllPackages Tests

        [Fact]
        public async Task GetAllPackages_ReturnsOkWithPackageList()
        {
            // Arrange
            var packages = new List<AutomationPackageResponseDto>
            {
                new AutomationPackageResponseDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Package1",
                    Description = "Description1"
                },
                new AutomationPackageResponseDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Package2",
                    Description = "Description2"
                }
            };

            _mockPackageService.Setup(x => x.GetAllPackagesAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.GetAllPackages();

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPackages = Assert.IsAssignableFrom<IEnumerable<AutomationPackageResponseDto>>(okResult.Value);
            Assert.Equal(2, ((List<AutomationPackageResponseDto>)returnedPackages).Count);
        }

        #endregion

        #region GetPackageDownloadUrl Tests

        [Fact]
        public async Task GetPackageDownloadUrl_WithValidData_ReturnsOkWithUrl()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";
            var downloadUrl = "https://example.com/download/package.zip";

            _mockPackageService.Setup(x => x.GetPackageDownloadUrlAsync(packageId, version))
                .ReturnsAsync(downloadUrl);

            // Act
            var result = await _controller.GetPackageDownloadUrl(packageId, version);

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetPackageDownloadUrl_WithInvalidData_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";

            _mockPackageService.Setup(x => x.GetPackageDownloadUrlAsync(packageId, version))
                .ThrowsAsync(new ArgumentException("Package version not found"));

            // Act
            var result = await _controller.GetPackageDownloadUrl(packageId, version);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Package version not found", notFoundResult.Value);
        }

        #endregion

        #region GetAgentDownloadUrl Tests

        [Fact]
        public async Task GetAgentDownloadUrl_WithValidMachineKey_ReturnsOkWithUrl()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";
            var machineKey = "valid-machine-key";
            var downloadUrl = "https://example.com/download/package.zip";

            var botAgents = new List<BotAgentResponseDto>
            {
                new BotAgentResponseDto
                {
                    Id = Guid.NewGuid(),
                    MachineKey = machineKey,
                    Name = "TestAgent"
                }
            };

            _mockBotAgentService.Setup(x => x.GetAllBotAgentsAsync())
                .ReturnsAsync(botAgents);
            _mockPackageService.Setup(x => x.GetPackageDownloadUrlAsync(packageId, version))
                .ReturnsAsync(downloadUrl);

            // Act
            var result = await _controller.GetAgentDownloadUrl(packageId, version, machineKey);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAgentDownloadUrl_WithInvalidMachineKey_ReturnsUnauthorized()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";
            var machineKey = "invalid-machine-key";

            var botAgents = new List<BotAgentResponseDto>();

            _mockBotAgentService.Setup(x => x.GetAllBotAgentsAsync())
                .ReturnsAsync(botAgents);

            // Act
            var result = await _controller.GetAgentDownloadUrl(packageId, version, machineKey);

            // Assert
            Assert.NotNull(result);
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid machine key", unauthorizedResult.Value);
        }

        #endregion

        #region UploadPackageVersion Tests

        [Fact]
        public async Task UploadPackageVersion_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "2.0.0";
            var fileName = "package-v2.zip";
            var fileContent = "updated package content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageVersionRequest
            {
                File = mockFile.Object,
                Version = version
            };

            var packageVersion = new PackageVersionResponseDto
            {
                Id = Guid.NewGuid(),
                VersionNumber = version
            };

            _mockPackageService.Setup(x => x.UploadPackageVersionAsync(
                packageId, It.IsAny<Stream>(), fileName, version))
                .ReturnsAsync(packageVersion);

            // Act
            var result = await _controller.UploadPackageVersion(packageId, request);

            // Assert
            Assert.NotNull(result.Result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
        }

        [Fact]
        public async Task UploadPackageVersion_WithNullFile_ReturnsBadRequest()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UploadPackageVersionRequest
            {
                File = null!,
                Version = "1.0.0"
            };

            // Act
            var result = await _controller.UploadPackageVersion(packageId, request);

            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("File is required", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadPackageVersion_WithEmptyVersion_ReturnsBadRequest()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);

            var request = new UploadPackageVersionRequest
            {
                File = mockFile.Object,
                Version = ""
            };

            // Act
            var result = await _controller.UploadPackageVersion(packageId, request);

            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Version is required", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadPackageVersion_WithInvalidPackageId_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";
            var fileName = "package.zip";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageVersionRequest
            {
                File = mockFile.Object,
                Version = version
            };

            _mockPackageService.Setup(x => x.UploadPackageVersionAsync(
                packageId, It.IsAny<Stream>(), fileName, version))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act
            var result = await _controller.UploadPackageVersion(packageId, request);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Package not found", notFoundResult.Value);
        }

        [Fact]
        public async Task UploadPackageVersion_WithDuplicateVersion_ReturnsConflict()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";
            var fileName = "package.zip";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var request = new UploadPackageVersionRequest
            {
                File = mockFile.Object,
                Version = version
            };

            _mockPackageService.Setup(x => x.UploadPackageVersionAsync(
                packageId, It.IsAny<Stream>(), fileName, version))
                .ThrowsAsync(new InvalidOperationException("Version already exists"));

            // Act
            var result = await _controller.UploadPackageVersion(packageId, request);

            // Assert
            Assert.NotNull(result.Result);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.NotNull(conflictResult.Value);
        }

        #endregion

        #region DeletePackage Tests

        [Fact]
        public async Task DeletePackage_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var packageId = Guid.NewGuid();

            _mockPackageService.Setup(x => x.DeletePackageAsync(packageId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePackage_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();

            _mockPackageService.Setup(x => x.DeletePackageAsync(packageId))
                .ThrowsAsync(new ArgumentException("Package not found"));

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            Assert.NotNull(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        #endregion

        #region DeletePackageVersion Tests

        [Fact]
        public async Task DeletePackageVersion_WithValidData_ReturnsNoContent()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";

            _mockPackageService.Setup(x => x.DeletePackageVersionAsync(packageId, version))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePackageVersion(packageId, version);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePackageVersion_WithInvalidData_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var version = "1.0.0";

            _mockPackageService.Setup(x => x.DeletePackageVersionAsync(packageId, version))
                .ThrowsAsync(new ArgumentException("Package version not found"));

            // Act
            var result = await _controller.DeletePackageVersion(packageId, version);

            // Assert
            Assert.NotNull(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        #endregion
    }
}
