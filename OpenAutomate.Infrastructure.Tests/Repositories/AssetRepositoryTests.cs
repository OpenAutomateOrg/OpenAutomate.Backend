using Microsoft.EntityFrameworkCore;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Repositories
{
    public class AssetRepositoryTests
    {
        // Using a fixed tenant ID for all tests so queries work properly with the tenant filter
        private readonly Guid _tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Create a real tenant context instead of a mock to avoid issues with the query filter
        private class TestTenantContext : ITenantContext
        {
            public Guid CurrentTenantId { get; set; }
            public bool HasTenant => true;
            
            public void SetTenant(Guid tenantId)
            {
                CurrentTenantId = tenantId;
            }
            
            public void ClearTenant()
            {
                // No-op for tests, we always want a tenant
            }
        }
        
        private readonly TestTenantContext _tenantContext;
        
        public AssetRepositoryTests()
        {
            _tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        }
        
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            return new ApplicationDbContext(options, _tenantContext);
        }
        
        [Fact]
        public async Task AddAsync_WithValidAsset_PersistsAsset()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            var asset = new Asset
            {
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            // Act
            await repository.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Assert
            var savedAsset = await context.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Key == "ApiKey");
            Assert.NotNull(savedAsset);
            Assert.Equal("secret-value-123", savedAsset.Value);
            Assert.Equal("API Key for external service", savedAsset.Description);
            Assert.True(savedAsset.IsEncrypted);
            Assert.Equal(_tenantId, savedAsset.OrganizationUnitId);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithExistingAsset_ReturnsAsset()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var result = await repository.GetByIdAsync(asset.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(asset.Id, result.Id);
            Assert.Equal("ApiKey", result.Key);
            Assert.Equal("secret-value-123", result.Value);
            Assert.Equal("API Key for external service", result.Description);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithNonExistingAsset_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            var nonExistingId = Guid.NewGuid();
            
            // Act
            var result = await repository.GetByIdAsync(nonExistingId);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetFirstOrDefaultAsync_WithExistingAsset_ReturnsAsset()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            var asset = new Asset
            {
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var result = await repository.GetFirstOrDefaultAsync(a => a.Key == "ApiKey");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("ApiKey", result.Key);
            Assert.Equal("secret-value-123", result.Value);
        }
        
        [Fact]
        public async Task Update_ModifiesExistingAsset_PersistsChanges()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            var asset = new Asset
            {
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Act
            asset.Value = "updated-secret-value";
            asset.Description = "Updated API Key description";
            repository.Update(asset);
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Assert
            var updatedAsset = await context.Assets.FirstOrDefaultAsync(a => a.Key == "ApiKey");
            Assert.NotNull(updatedAsset);
            Assert.Equal("updated-secret-value", updatedAsset.Value);
            Assert.Equal("Updated API Key description", updatedAsset.Description);
        }
        
        [Fact]
        public async Task Remove_ExistingAsset_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            var asset = new Asset
            {
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Act
            repository.Remove(asset);
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Assert
            var deletedAsset = await context.Assets.FirstOrDefaultAsync(a => a.Key == "ApiKey");
            Assert.Null(deletedAsset);
        }
        
        [Fact]
        public async Task GetAllAsync_WithMultipleAssets_ReturnsFilteredAssets()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            await context.Assets.AddAsync(new Asset { Key = "ApiKey1", Value = "value1", OrganizationUnitId = _tenantId });
            await context.Assets.AddAsync(new Asset { Key = "ApiKey2", Value = "value2", OrganizationUnitId = _tenantId });
            await context.Assets.AddAsync(new Asset { Key = "DatabaseSecret", Value = "secret", OrganizationUnitId = _tenantId });
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var assets = await repository.GetAllAsync(a => a.Key.StartsWith("Api"));
            
            // Assert
            Assert.Equal(2, assets.Count());
            Assert.Contains(assets, a => a.Key == "ApiKey1");
            Assert.Contains(assets, a => a.Key == "ApiKey2");
            Assert.DoesNotContain(assets, a => a.Key == "DatabaseSecret");
        }
        
        [Fact]
        public async Task GetAssetsByBotId_ReturnsAssociatedAssets()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            // Create a bot agent
            var botAgent = new BotAgent
            {
                Name = "TestBot",
                MachineKey = "machine-key-123",
                MachineName = "Test-Machine",
                OrganizationUnitId = _tenantId
            };
            await context.BotAgents.AddAsync(botAgent);
            
            // Create assets
            var asset1 = new Asset { Key = "ApiKey1", Value = "value1", OrganizationUnitId = _tenantId };
            var asset2 = new Asset { Key = "ApiKey2", Value = "value2", OrganizationUnitId = _tenantId };
            var asset3 = new Asset { Key = "DatabaseSecret", Value = "secret", OrganizationUnitId = _tenantId };
            
            await context.Assets.AddRangeAsync(asset1, asset2, asset3);
            await context.SaveChangesAsync();
            
            // Link assets to bot
            await context.AssetBotAgents.AddRangeAsync(
                new AssetBotAgent { AssetId = asset1.Id, BotAgentId = botAgent.Id },
                new AssetBotAgent { AssetId = asset2.Id, BotAgentId = botAgent.Id }
            );
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var assets = await context.Assets
                .Include(a => a.AssetBotAgents)
                .Where(a => a.AssetBotAgents != null && a.AssetBotAgents.Any(aba => aba.BotAgentId == botAgent.Id))
                .ToListAsync();
            
            // Assert
            Assert.Equal(2, assets.Count);
            Assert.Contains(assets, a => a.Key == "ApiKey1");
            Assert.Contains(assets, a => a.Key == "ApiKey2");
            Assert.DoesNotContain(assets, a => a.Key == "DatabaseSecret");
        }
        
        [Fact]
        public async Task GetEncryptedAssets_ReturnsOnlyEncryptedAssets()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            await context.Assets.AddAsync(new Asset { 
                Key = "ApiKey", 
                Value = "value1", 
                IsEncrypted = true,
                OrganizationUnitId = _tenantId 
            });
            
            await context.Assets.AddAsync(new Asset { 
                Key = "AppSetting", 
                Value = "value2", 
                IsEncrypted = false,
                OrganizationUnitId = _tenantId 
            });
            
            await context.Assets.AddAsync(new Asset { 
                Key = "Password", 
                Value = "secret", 
                IsEncrypted = true,
                OrganizationUnitId = _tenantId 
            });
            
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var encryptedAssets = await repository.GetAllAsync(a => a.IsEncrypted);
            
            // Assert
            Assert.Equal(2, encryptedAssets.Count());
            Assert.Contains(encryptedAssets, a => a.Key == "ApiKey");
            Assert.Contains(encryptedAssets, a => a.Key == "Password");
            Assert.DoesNotContain(encryptedAssets, a => a.Key == "AppSetting");
        }
        
        [Fact]
        public async Task GetAssetByKey_ReturnsCorrectAsset()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<Asset>(context);
            
            await context.Assets.AddAsync(new Asset { 
                Key = "ApiKey", 
                Value = "value1",
                OrganizationUnitId = _tenantId 
            });
            
            await context.Assets.AddAsync(new Asset { 
                Key = "AppSetting", 
                Value = "value2",
                OrganizationUnitId = _tenantId 
            });
            
            await context.SaveChangesAsync();
            
            // Clear tracking
            context.ChangeTracker.Clear();
            
            // Act
            var asset = await repository.GetFirstOrDefaultAsync(a => a.Key == "ApiKey");
            
            // Assert
            Assert.NotNull(asset);
            Assert.Equal("ApiKey", asset.Key);
            Assert.Equal("value1", asset.Value);
        }
        
        [Fact]
        public async Task AddAssetWithBotAgentLink_StoresRelationship()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var assetRepository = new Repository<Asset>(context);
            var assetBotAgentRepository = new Repository<AssetBotAgent>(context);
            
            // Create a bot agent
            var botAgent = new BotAgent
            {
                Name = "TestBot",
                MachineKey = "machine-key-123",
                MachineName = "Test-Machine",
                OrganizationUnitId = _tenantId
            };
            await context.BotAgents.AddAsync(botAgent);
            await context.SaveChangesAsync();
            
            // Create an asset
            var asset = new Asset
            {
                Key = "ApiKey",
                Value = "secret-value-123",
                Description = "API Key for external service",
                IsEncrypted = true,
                OrganizationUnitId = _tenantId
            };
            
            await assetRepository.AddAsync(asset);
            await context.SaveChangesAsync();
            
            // Act - link bot agent to asset
            var assetBotAgent = new AssetBotAgent
            {
                AssetId = asset.Id,
                BotAgentId = botAgent.Id
            };
            
            await assetBotAgentRepository.AddAsync(assetBotAgent);
            await context.SaveChangesAsync();
            
            // Clear tracking  ensure we get a fresh read
            context.ChangeTracker.Clear();
            
            // Assert
            var savedAsset = await context.Assets
                .Include(a => a.AssetBotAgents)
                .FirstOrDefaultAsync(a => a.Key == "ApiKey");
                
            Assert.NotNull(savedAsset);
            Assert.NotNull(savedAsset.AssetBotAgents);
            Assert.Single(savedAsset.AssetBotAgents);
            Assert.Equal(botAgent.Id, savedAsset.AssetBotAgents.First().BotAgentId);
        }
    }
} 