using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Services
{
    public class TenantQueryFilterServiceTests
    {
        private class TestTenantContext : ITenantContext
        {
            public Guid CurrentTenantId { get; private set; }
            public bool HasTenant { get; private set; }
            public void SetTenant(Guid tenantId)
            {
                CurrentTenantId = tenantId;
                HasTenant = true;
            }
            public void ClearTenant() { HasTenant = false; }
            public Task<bool> ResolveTenantFromSlugAsync(string slug) => Task.FromResult(true);
        }

        private static readonly InMemoryDatabaseRoot _dbRoot = new();

        private ApplicationDbContext CreateContext(TestTenantContext tenantContext, string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString(), _dbRoot)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .EnableSensitiveDataLogging()
                .Options;
            return new ApplicationDbContext(options, tenantContext);
        }

        [Fact]
        public async Task QueryFilter_WithTenant_ReturnsOnlyTenantEntities()
        {
            var tenantId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            var tenantContext = new TestTenantContext();
            tenantContext.SetTenant(tenantId);
            using var context = CreateContext(tenantContext);
            context.Assets.Add(new Asset { Key = "a1", Value = "v1", OrganizationUnitId = tenantId });
            context.Assets.Add(new Asset { Key = "a2", Value = "v2", OrganizationUnitId = otherId });
            await context.SaveChangesAsync();

            var assets = await context.Assets.ToListAsync();
            Assert.Single(assets);
            Assert.Equal(tenantId, assets[0].OrganizationUnitId);
        }

        [Fact]
        public async Task QueryFilter_NoTenant_ReturnsAllEntities()
        {
            var tenantId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            var tenantContext = new TestTenantContext();
            using var context = CreateContext(tenantContext);
            context.Assets.Add(new Asset { Key = "a1", Value = "v1", OrganizationUnitId = tenantId });
            context.Assets.Add(new Asset { Key = "a2", Value = "v2", OrganizationUnitId = otherId });
            await context.SaveChangesAsync();

            var assets = await context.Assets.ToListAsync();
            Assert.Equal(2, assets.Count);
        }
    }
}
