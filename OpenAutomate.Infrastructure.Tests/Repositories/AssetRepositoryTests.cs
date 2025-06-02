using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Repositories
{
    public class AssetRepositoryTests
    {
        // Using fixed tenant IDs for tests
        private readonly Guid _tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly Guid _otherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");        // Custom tenant context for testing
        private class TestTenantContext : ITenantContext
        {
            public Guid CurrentTenantId { get; set; }
            public string? CurrentTenantSlug { get; set; }
            public bool HasTenant => true;

            public void SetTenant(Guid tenantId)
            {
                CurrentTenantId = tenantId;
            }

            public void ClearTenant()
            {
                // No-op for tests - we always want a tenant
            }

            public Task<bool> ResolveTenantFromSlugAsync(string tenantSlug)
            {
                // For testing purposes, just return true if we have a valid slug
                return Task.FromResult(!string.IsNullOrEmpty(tenantSlug));
            }
        }

        private readonly TestTenantContext _tenantContext;

        public AssetRepositoryTests()
        {
            _tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        }


        private ApplicationDbContext GetInMemoryDbContext(string? dbName = null)
        {
            // Create a unique database name for each test if not provided
            dbName = dbName ?? Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new ApplicationDbContext(options, _tenantContext);
        }

        [Fact]
        public async Task AddAsync_ValidAsset_Persists()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(context);


            var asset = new Asset
            {
                Key = "key1",
                Value = "val1",

                OrganizationUnitId = _tenantId,
                Description = "Test asset"
            };

            // Act
            await repo.AddAsync(asset);
            await context.SaveChangesAsync();



            // Assert - Use a new context to verify data was saved
            using var assertContext = GetInMemoryDbContext(dbName);
            var saved = await assertContext.Assets
                .Where(a => a.Key == "key1")
                .FirstOrDefaultAsync();



            Assert.NotNull(saved);
            Assert.Equal("val1", saved.Value);
            Assert.Equal("Test asset", saved.Description);
        }

        [Fact]
        public async Task AddAsync_NullAsset_ThrowsArgumentNullException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repo = new Repository<Asset>(context);

            // Act & Assert

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.AddAsync(null));
#pragma warning restore CS8625
        }


        [Fact]
        public async Task GetByIdAsync_ExistingAsset_ReturnsAsset()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);


            var assetId = Guid.NewGuid();
            var asset = new Asset
            {
                Id = assetId,
                Key = "key2",
                Value = "val2",
                OrganizationUnitId = _tenantId
            };

            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            // Act - Use a new context since we're testing the repository
            using var actContext = GetInMemoryDbContext(dbName);
            var actRepo = new Repository<Asset>(actContext);
            var found = await actRepo.GetByIdAsync(assetId);

            // Assert
            Assert.NotNull(found);
            Assert.Equal("key2", found.Key);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingAsset_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repo = new Repository<Asset>(context);

            // Act
            var found = await repo.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(found);
        }

        [Fact]
        public async Task Update_ExistingAsset_PersistsChanges()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            var asset = new Asset
            {
                Key = "key3",
                Value = "original",
                OrganizationUnitId = _tenantId
            };

            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            // Create a separate context for the update operation
            using var updateContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(updateContext);


            // Get the entity to update
            var assetToUpdate = await updateContext.Assets
                .Where(a => a.Key == "key3")
                .FirstAsync();


            // Modify and update
            assetToUpdate.Value = "updated";
            repo.Update(assetToUpdate);
            await updateContext.SaveChangesAsync();

            // Assert - Use new context to verify
            using var assertContext = GetInMemoryDbContext(dbName);
            var updated = await assertContext.Assets
                .Where(a => a.Key == "key3")
                .FirstOrDefaultAsync();


            Assert.NotNull(updated);
            Assert.Equal("updated", updated.Value);
        }


        [Fact]
        public async Task Remove_ExistingAsset_RemovesFromDatabase()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            var asset = new Asset
            {
                Key = "key4",
                Value = "val4",
                OrganizationUnitId = _tenantId
            };

            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            // Create a separate context for the remove operation
            using var removeContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(removeContext);

            // Get the entity to remove
            var assetToDelete = await removeContext.Assets
                .Where(a => a.Key == "key4")
                .FirstAsync();

            repo.Remove(assetToDelete);
            await removeContext.SaveChangesAsync();

            // Assert
            using var assertContext = GetInMemoryDbContext(dbName);
            var deleted = await assertContext.Assets
                .Where(a => a.Key == "key4")
                .FirstOrDefaultAsync();

            Assert.Null(deleted);
        }

        [Fact]
        public async Task GetAllAsync_WithFilter_ReturnsMatchingAssets()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            await context.Assets.AddRangeAsync(new[] {
                new Asset { Key = "a1", Value = "v1", OrganizationUnitId = _tenantId },
                new Asset { Key = "a2", Value = "v2", OrganizationUnitId = _tenantId },
                new Asset { Key = "b1", Value = "v3", OrganizationUnitId = _tenantId }
            });

            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var assets = await repo.GetAllAsync(a => a.Key.StartsWith('a'));

            // Assert
            Assert.Equal(2, assets.Count());
            Assert.All(assets, a => Assert.StartsWith("a", a.Key));
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_ExistingMatch_ReturnsAsset()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            await context.Assets.AddAsync(new Asset
            {
                Key = "findme",
                Value = "v",
                OrganizationUnitId = _tenantId
            });

            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var asset = await repo.GetFirstOrDefaultAsync(a => a.Key == "findme");

            // Assert
            Assert.NotNull(asset);
            Assert.Equal("findme", asset.Key);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_NoMatch_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repo = new Repository<Asset>(context);

            // Act
            var asset = await repo.GetFirstOrDefaultAsync(a => a.Key == "notfound");

            // Assert
            Assert.Null(asset);
        }

        [Fact]
        public async Task AnyAsync_ExistingMatch_ReturnsTrue()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            await context.Assets.AddAsync(new Asset
            {
                Key = "exists",
                Value = "v",
                OrganizationUnitId = _tenantId
            });

            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var any = await repo.AnyAsync(a => a.Key == "exists");

            // Assert
            Assert.True(any);
        }

        [Fact]
        public async Task AnyAsync_NoMatch_ReturnsFalse()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repo = new Repository<Asset>(context);

            // Act
            var any = await repo.AnyAsync(a => a.Key == "notfound");

            // Assert
            Assert.False(any);
        }

        [Fact]
        public async Task AddRangeAsync_MultipleAssets_AllArePersisted()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(context);

            var assets = new List<Asset>
            {
                new Asset { Key = "batch1", Value = "value1", OrganizationUnitId = _tenantId },
                new Asset { Key = "batch2", Value = "value2", OrganizationUnitId = _tenantId },
                new Asset { Key = "batch3", Value = "value3", OrganizationUnitId = _tenantId }
            };

            // Act
            await repo.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Assert
            using var assertContext = GetInMemoryDbContext(dbName);
            var savedAssets = await assertContext.Assets
                .Where(a => a.Key.StartsWith("batch"))
                .OrderBy(a => a.Key)
                .ToListAsync();

            Assert.Equal(3, savedAssets.Count);
            Assert.Equal("batch1", savedAssets[0].Key);
            Assert.Equal("batch2", savedAssets[1].Key);
            Assert.Equal("batch3", savedAssets[2].Key);
        }

        [Fact]
        public async Task GetAllAsync_WithOrderBy_ReturnsOrderedAssets()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            await context.Assets.AddRangeAsync(new[]
            {
                new Asset { Key = "z_key", Value = "z_val", OrganizationUnitId = _tenantId },
                new Asset { Key = "a_key", Value = "a_val", OrganizationUnitId = _tenantId },
                new Asset { Key = "m_key", Value = "m_val", OrganizationUnitId = _tenantId }
            });

            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            var assets = await repo.GetAllAsync(
                null, // No filter
                q => q.OrderBy(a => a.Key) // Order by Key ascending
            );
#pragma warning restore CS8625

            // Assert
            var assetArray = assets.ToArray();
            Assert.Equal(3, assetArray.Length);
            Assert.Equal("a_key", assetArray[0].Key);
            Assert.Equal("m_key", assetArray[1].Key);
            Assert.Equal("z_key", assetArray[2].Key);
        }

        [Fact]
        public async Task GetAllAsync_WithInclude_IncludesRelatedEntities()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            // Create a bot agent
            var botAgent = new BotAgent
            {
                Id = Guid.NewGuid(),
                Name = "TestBot",
                MachineName = "test-machine",
                OrganizationUnitId = _tenantId
            };

            await context.BotAgents.AddAsync(botAgent);

            // Create an asset
            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                Key = "api_key",
                Value = "secret123",
                OrganizationUnitId = _tenantId
            };

            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();

            // Link bot agent to asset
            var assetBotAgent = new AssetBotAgent
            {
                AssetId = asset.Id,
                BotAgentId = botAgent.Id,
                OrganizationUnitId = _tenantId
            };


            await context.AssetBotAgents.AddAsync(assetBotAgent);
            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            var assets = await repo.GetAllAsync(
                a => a.Key == "api_key",
                null,
                a => a.AssetBotAgents
            );
#pragma warning restore CS8625

            // Assert
            var result = assets.Single();
            Assert.Equal("api_key", result.Key);
            Assert.NotNull(result.AssetBotAgents);
            Assert.Single(result.AssetBotAgents);
            Assert.Equal(botAgent.Id, result.AssetBotAgents.First().BotAgentId);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithInclude_IncludesRelatedEntities()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            // Create a bot agent
            var botAgent = new BotAgent
            {
                Id = Guid.NewGuid(),
                Name = "TestBot",
                MachineName = "test-machine",
                OrganizationUnitId = _tenantId
            };

            await context.BotAgents.AddAsync(botAgent);

            // Create an asset
            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                Key = "secret_key",
                Value = "secret456",
                OrganizationUnitId = _tenantId
            };

            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();

            // Link bot agent to asset
            var assetBotAgent = new AssetBotAgent
            {
                AssetId = asset.Id,
                BotAgentId = botAgent.Id,
                OrganizationUnitId = _tenantId
            };

            await context.AssetBotAgents.AddAsync(assetBotAgent);
            await context.SaveChangesAsync();

            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var result = await repo.GetFirstOrDefaultAsync(
                a => a.Key == "secret_key",
                a => a.AssetBotAgents
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal("secret_key", result.Key);
            Assert.NotNull(result.AssetBotAgents);
            Assert.Single(result.AssetBotAgents);
        }

        [Fact]
        public async Task RemoveRange_MultipleAssets_AllAreRemoved()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            var assets = new List<Asset>
            {
                new Asset { Key = "remove1", Value = "value1", OrganizationUnitId = _tenantId },
                new Asset { Key = "remove2", Value = "value2", OrganizationUnitId = _tenantId },
                new Asset { Key = "keep1", Value = "value3", OrganizationUnitId = _tenantId }
            };

            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            using var removeContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(removeContext);

            // Get entities to remove
            var assetsToRemove = await removeContext.Assets
                .Where(a => a.Key.StartsWith("remove"))
                .ToListAsync();

            Assert.Equal(2, assetsToRemove.Count);

            repo.RemoveRange(assetsToRemove);
            await removeContext.SaveChangesAsync();

            // Assert
            using var assertContext = GetInMemoryDbContext(dbName);
            var remainingAssets = await assertContext.Assets.ToListAsync();

            Assert.Single(remainingAssets);
            Assert.Equal("keep1", remainingAssets[0].Key);
        }

        [Fact]
        public async Task UpdateRange_MultipleAssets_AllChangesArePersisted()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);

            var assets = new List<Asset>
            {
                new Asset { Key = "update1", Value = "old1", OrganizationUnitId = _tenantId },
                new Asset { Key = "update2", Value = "old2", OrganizationUnitId = _tenantId },
                new Asset { Key = "update3", Value = "old3", OrganizationUnitId = _tenantId }
            };

            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();

            // Act
            using var updateContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(updateContext);

            // Get entities to update
            var assetsToUpdate = await updateContext.Assets
                .Where(a => a.Key.StartsWith("update"))
                .ToListAsync();

            Assert.Equal(3, assetsToUpdate.Count);

            // Modify each asset
            foreach (var asset in assetsToUpdate)
            {
                asset.Value = $"new{asset.Key.Last()}";
            }

            repo.UpdateRange(assetsToUpdate);
            await updateContext.SaveChangesAsync();

            // Assert

            using var assertContext = GetInMemoryDbContext(dbName);
            var updatedAssets = await assertContext.Assets
                .OrderBy(a => a.Key)
                .ToListAsync();


            Assert.Equal(3, updatedAssets.Count);
            Assert.Equal("new1", updatedAssets[0].Value);
            Assert.Equal("new2", updatedAssets[1].Value);
            Assert.Equal("new3", updatedAssets[2].Value);
        }

        [Fact]
        public async Task TenantFiltering_MultiTenant_OnlyReturnsTenantData()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();



            // Add data for the current tenant
            using (var context = GetInMemoryDbContext(dbName))
            {
                await context.Assets.AddAsync(new Asset

                {
                    Key = "tenant1",
                    Value = "val1",
                    OrganizationUnitId = _tenantId
                });
                await context.SaveChangesAsync();
            }


            // Add data for a different tenant
            var otherTenantContext = new TestTenantContext { CurrentTenantId = _otherTenantId };
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;


            using (var otherDbContext = new ApplicationDbContext(options, otherTenantContext))
            {
                await otherDbContext.Assets.AddAsync(new Asset
                {
                    Key = "tenant2",
                    Value = "val2",
                    OrganizationUnitId = _otherTenantId
                });
                await otherDbContext.SaveChangesAsync();
            }

            // Act - Query with original tenant context
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var assets = await repo.GetAllAsync();

            // Assert - Should only see current tenant's data
            Assert.Single(assets);
            Assert.Equal("tenant1", assets.First().Key);
            Assert.DoesNotContain(assets, a => a.Key == "tenant2");
        }

        [Fact]
        public async Task GetAllAsync_NoFilter_ReturnsAllTenantAssets()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = GetInMemoryDbContext(dbName);



            await context.Assets.AddRangeAsync(new[]
            {
                new Asset { Key = "key1", Value = "val1", OrganizationUnitId = _tenantId },
                new Asset { Key = "key2", Value = "val2", OrganizationUnitId = _tenantId },
                new Asset { Key = "key3", Value = "val3", OrganizationUnitId = _tenantId }
            });


            await context.SaveChangesAsync();


            // Act
            using var queryContext = GetInMemoryDbContext(dbName);
            var repo = new Repository<Asset>(queryContext);
            var assets = await repo.GetAllAsync();

            // Assert
            Assert.Equal(3, assets.Count());
        }
    }
}