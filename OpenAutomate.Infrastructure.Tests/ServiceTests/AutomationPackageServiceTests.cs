using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Infrastructure.Services;
using System.Linq.Expressions;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class AutomationPackageServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<IPackageStorageService> _mockStorageService;
        private readonly Mock<ILogger<AutomationPackageService>> _mockLogger;
        private readonly IOptions<AwsSettings> _awsOptions;
        private readonly AutomationPackageService _service;

        public AutomationPackageServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockStorageService = new Mock<IPackageStorageService>();
            _mockLogger = new Mock<ILogger<AutomationPackageService>>();
            _awsOptions = Options.Create(new AwsSettings { PresignedUrlExpirationMinutes = 60 });

            _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(Guid.NewGuid());
            _mockTenantContext.Setup(t => t.CurrentTenantSlug).Returns("tenant-slug");

            _service = new AutomationPackageService(
                _mockUnitOfWork.Object,
                _mockTenantContext.Object,
                _mockStorageService.Object,
                _awsOptions,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreatePackageAsync_ShouldCreate_WhenNameIsUnique()
        {
            // Arrange
            var dto = new CreateAutomationPackageDto { Name = "pkg", Description = "desc" };
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AutomationPackage, bool>>>(), null))
                .ReturnsAsync((AutomationPackage?)null);

            _mockUnitOfWork.Setup(u => u.AutomationPackages.AddAsync(It.IsAny<AutomationPackage>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreatePackageAsync(dto);

            // Assert
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Description, result.Description);
        }



        [Fact]
        public async Task GetPackageByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AutomationPackage?)null);

            // Act
            var result = await _service.GetPackageByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPackageByIdAsync_ShouldReturnPackage_WhenFound()
        {
            // Arrange
            var pkgId = Guid.NewGuid();
            var tenantId = _mockTenantContext.Object.CurrentTenantId;
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetByIdAsync(pkgId))
                .ReturnsAsync(new AutomationPackage { Id = pkgId, OrganizationUnitId = tenantId, Name = "pkg" });

            _mockUnitOfWork.Setup(u => u.PackageVersions.GetAllAsync(
                It.IsAny<Expression<Func<PackageVersion, bool>>>(), null, null))
                .ReturnsAsync(new List<PackageVersion>());

            // Act
            var result = await _service.GetPackageByIdAsync(pkgId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pkgId, result.Id);
        }

        [Fact]
        public async Task UploadPackageVersionAsync_ShouldThrow_WhenPackageNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AutomationPackage?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadPackageVersionAsync(Guid.NewGuid(), new MemoryStream(), "file.zip", "1.0.0"));
        }

        [Fact]
        public async Task GetPackageDownloadUrlAsync_ShouldThrow_WhenVersionNotFound()
        {
            // Arrange
            var pkgId = Guid.NewGuid();
            var tenantId = _mockTenantContext.Object.CurrentTenantId;
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetByIdAsync(pkgId))
                .ReturnsAsync(new AutomationPackage { Id = pkgId, OrganizationUnitId = tenantId });

            _mockUnitOfWork.Setup(u => u.PackageVersions.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<PackageVersion, bool>>>(), null))
                .ReturnsAsync((PackageVersion?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetPackageDownloadUrlAsync(pkgId, "1.0.0"));
        }

        [Fact]
        public async Task DeletePackageAsync_ShouldThrow_WhenNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AutomationPackage?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DeletePackageAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task PackageVersionExistsAsync_ShouldReturnFalse_WhenPackageNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.AutomationPackages.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<AutomationPackage, bool>>>(), null))
                .ReturnsAsync((AutomationPackage?)null);

            // Act
            var result = await _service.PackageVersionExistsAsync("pkg", "1.0.0");

            // Assert
            Assert.False(result);
        }
    }
}
