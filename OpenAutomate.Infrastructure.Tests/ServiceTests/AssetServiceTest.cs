using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Infrastructure.Services;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class AssetServiceTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<AssetService>> _mockLogger;
        private readonly AssetService _service;
        private readonly Guid _tenantId;

        public AssetServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options, Mock.Of<ITenantContext>());
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<AssetService>>();
            _service = new AssetService(_dbContext, _mockTenantContext.Object, _mockLogger.Object);
            _tenantId = Guid.NewGuid();
            _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(_tenantId);
        }

        [Fact]
        public async Task CreateAssetAsync_Success()
        {
            var dto = new CreateAssetDto { Key = "key1", Value = "val", Type = AssetType.String };
            var result = await _service.CreateAssetAsync(dto);
            Assert.NotNull(result);
            Assert.Equal("key1", result.Key);
        }

        [Fact]
        public async Task CreateAssetAsync_DuplicateKey_Throws()
        {
            var dto = new CreateAssetDto { Key = "dup", Value = "val", Type = AssetType.String };
            _dbContext.Assets.Add(new Asset { Key = "dup", OrganizationUnitId = _tenantId });
            await _dbContext.SaveChangesAsync();
            await Assert.ThrowsAsync<AssetKeyAlreadyExistsException>(() => _service.CreateAssetAsync(dto));
        }

        [Fact]
        public async Task GetAllAssetsAsync_ReturnsAssets()
        {
            _dbContext.Assets.Add(new Asset { Id = Guid.NewGuid(), Key = "k", OrganizationUnitId = _tenantId });
            await _dbContext.SaveChangesAsync();
            var result = await _service.GetAllAssetsAsync();
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAssetByIdAsync_Found()
        {
            var assetId = Guid.NewGuid();
            _dbContext.Assets.Add(new Asset { Id = assetId, Key = "k", OrganizationUnitId = _tenantId });
            await _dbContext.SaveChangesAsync();
            var result = await _service.GetAssetByIdAsync(assetId);
            Assert.NotNull(result);
            Assert.Equal(assetId, result.Id);
        }

        [Fact]
        public async Task GetAssetByIdAsync_NotFound_ReturnsNull()
        {
            var result = await _service.GetAssetByIdAsync(Guid.NewGuid());
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAssetAsync_Success()
        {
            var assetId = Guid.NewGuid();
            _dbContext.Assets.Add(new Asset { Id = assetId, Key = "k", OrganizationUnitId = _tenantId, Value = "v" });
            await _dbContext.SaveChangesAsync();
            var dto = new UpdateAssetDto { Key = "k2", Value = "v2", Description = "desc" };
            var result = await _service.UpdateAssetAsync(assetId, dto);
            Assert.NotNull(result);
            Assert.Equal("k2", result.Key);
        }

        [Fact]
        public async Task UpdateAssetAsync_NotFound_Throws()
        {
            var dto = new UpdateAssetDto { Key = "k", Value = "v", Description = "desc" };
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAssetAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task DeleteAssetAsync_Success()
        {
            var assetId = Guid.NewGuid();
            _dbContext.Assets.Add(new Asset { Id = assetId, Key = "k", OrganizationUnitId = _tenantId });
            await _dbContext.SaveChangesAsync();
            var result = await _service.DeleteAssetAsync(assetId);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAssetAsync_NotFound_ReturnsFalse()
        {
            var result = await _service.DeleteAssetAsync(Guid.NewGuid());
            Assert.False(result);
        }

        // Bạn có thể bổ sung thêm các test cho các method còn lại nếu cần thiết.
    }
}
