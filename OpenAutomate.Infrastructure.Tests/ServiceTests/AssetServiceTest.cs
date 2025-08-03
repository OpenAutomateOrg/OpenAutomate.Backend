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
using System.Linq;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    /// <summary>
    /// Test-specific ApplicationDbContext that doesn't apply tenant query filters
    /// </summary>
    public class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantContext tenantContext) : base(options, tenantContext)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations without tenant filters
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationUnitConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationUnitUserConfiguration());
            modelBuilder.ApplyConfiguration(new AuthorityConfiguration());
            modelBuilder.ApplyConfiguration(new UserAuthorityConfiguration());
            modelBuilder.ApplyConfiguration(new AuthorityResourceConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new BotAgentConfiguration());
            modelBuilder.ApplyConfiguration(new AutomationPackageConfiguration());
            modelBuilder.ApplyConfiguration(new PackageVersionConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionConfiguration());
            modelBuilder.ApplyConfiguration(new AssetConfiguration());
            modelBuilder.ApplyConfiguration(new AssetBotAgentConfiguration());
            modelBuilder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
            modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
            modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
            
            // Configure all tenant entities to use NoAction for OrganizationUnit to prevent cascade cycles
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var foreignKey in entityType.GetForeignKeys()
                    .Where(fk => fk.PrincipalEntityType.ClrType == typeof(OrganizationUnit) && 
                                 fk.Properties.Count == 1 && 
                                 fk.Properties.First().Name == "OrganizationUnitId"))
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
                }
            }
            
            // Skip tenant query filters for testing - DO NOT apply _tenantQueryFilterService.ApplyTenantFilters(modelBuilder);
        }
    }

    public class AssetServiceTests : IDisposable
    {
        private readonly TestApplicationDbContext _dbContext;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<AssetService>> _mockLogger;
        private readonly AssetService _service;
        private readonly Guid _tenantId;

        public AssetServiceTests()
        {
            _tenantId = Guid.NewGuid();
            
            // Setup tenant context mock
            _mockTenantContext = new Mock<ITenantContext>();
            _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(_tenantId);
            _mockTenantContext.Setup(t => t.HasTenant).Returns(true);
            
            // Create in-memory database options
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            // Create test-specific DbContext without tenant filters
            _dbContext = new TestApplicationDbContext(options, _mockTenantContext.Object);
            
            // Ensure database is created
            _dbContext.Database.EnsureCreated();
            
            // Setup logger mock
            _mockLogger = new Mock<ILogger<AssetService>>();
            
            // Create service
            _service = new AssetService(_dbContext, _mockTenantContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateAssetAsync_Success()
        {
            // Arrange
            var dto = new CreateAssetDto { Key = "key1", Value = "val", Type = AssetType.String };
            
            // Act
            var result = await _service.CreateAssetAsync(dto);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("key1", result.Key);
            Assert.Equal("val", result.Value);
        }

        [Fact]
        public async Task CreateAssetAsync_DuplicateKey_Throws()
        {
            // Arrange
            var dto = new CreateAssetDto { Key = "dup", Value = "val", Type = AssetType.String };
            
            // Add existing asset with same key
            var existingAsset = new Asset 
            { 
                Id = Guid.NewGuid(),
                Key = "dup", 
                Value = "existing_value",
                Description = "existing description",
                IsEncrypted = false,
                OrganizationUnitId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Assets.Add(existingAsset);
            await _dbContext.SaveChangesAsync();
            
            // Act & Assert
            await Assert.ThrowsAsync<AssetKeyAlreadyExistsException>(() => _service.CreateAssetAsync(dto));
        }

        [Fact]
        public async Task GetAllAssetsAsync_ReturnsAssets()
        {
            // Arrange
            var asset = new Asset 
            { 
                Id = Guid.NewGuid(), 
                Key = "test_key", 
                Value = "test_value",
                Description = "test description",
                IsEncrypted = false,
                OrganizationUnitId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Assets.Add(asset);
            await _dbContext.SaveChangesAsync();
            
            // Act
            var result = await _service.GetAllAssetsAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test_key", result.First().Key);
        }

        [Fact]
        public async Task GetAssetByIdAsync_Found()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = new Asset 
            { 
                Id = assetId, 
                Key = "test_key", 
                Value = "test_value",
                Description = "test description",
                IsEncrypted = false,
                OrganizationUnitId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Assets.Add(asset);
            await _dbContext.SaveChangesAsync();
            
            // Act
            var result = await _service.GetAssetByIdAsync(assetId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(assetId, result.Id);
            Assert.Equal("test_key", result.Key);
        }

        [Fact]
        public async Task GetAssetByIdAsync_NotFound_ReturnsNull()
        {
            // Act
            var result = await _service.GetAssetByIdAsync(Guid.NewGuid());
            
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAssetAsync_Success()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = new Asset 
            { 
                Id = assetId, 
                Key = "original_key", 
                Value = "original_value",
                Description = "original description",
                IsEncrypted = false,
                OrganizationUnitId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Assets.Add(asset);
            await _dbContext.SaveChangesAsync();
            
            var dto = new UpdateAssetDto 
            { 
                Key = "updated_key", 
                Value = "updated_value", 
                Description = "updated description" 
            };
            
            // Act
            var result = await _service.UpdateAssetAsync(assetId, dto);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("updated_key", result.Key);
            Assert.Equal("updated_value", result.Value);
            Assert.Equal("updated description", result.Description);
        }

        [Fact]
        public async Task UpdateAssetAsync_NotFound_Throws()
        {
            // Arrange
            var dto = new UpdateAssetDto { Key = "k", Value = "v", Description = "desc" };
            
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAssetAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task DeleteAssetAsync_Success()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = new Asset 
            { 
                Id = assetId, 
                Key = "test_key", 
                Value = "test_value",
                Description = "test description",
                IsEncrypted = false,
                OrganizationUnitId = _tenantId,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.Assets.Add(asset);
            await _dbContext.SaveChangesAsync();
            
            // Act
            var result = await _service.DeleteAssetAsync(assetId);
            
            // Assert
            Assert.True(result);
            
            // Verify asset is deleted
            var deletedAsset = await _dbContext.Assets.FindAsync(assetId);
            Assert.Null(deletedAsset);
        }

        [Fact]
        public async Task DeleteAssetAsync_NotFound_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteAssetAsync(Guid.NewGuid());
            
            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
