using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Services
{
    public class TenantContextTests
    {
        private ServiceProvider BuildProvider(Mock<IUnitOfWork> mockUow)
        {
            var services = new ServiceCollection();
            services.AddScoped<IUnitOfWork>(_ => mockUow.Object);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void SetAndClearTenant_UpdatesState()
        {
            var logger = Mock.Of<ILogger<TenantContext>>();
            var context = new TenantContext(logger, new ServiceCollection().BuildServiceProvider());
            var id = Guid.NewGuid();
            context.SetTenant(id);
            Assert.True(context.HasTenant);
            Assert.Equal(id, context.CurrentTenantId);
            context.ClearTenant();
            Assert.False(context.HasTenant);
            Assert.Throws<InvalidOperationException>(() => { var _ = context.CurrentTenantId; });
        }

        [Fact]
        public async Task ResolveTenantFromSlugAsync_SetsTenantWhenFound()
        {
            var tenantId = Guid.NewGuid();
            var repo = new Mock<IRepository<OrganizationUnit>>();
            repo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<OrganizationUnit, bool>>>(), null))
                .ReturnsAsync(new OrganizationUnit { Id = tenantId, Name = "Test", Slug = "slug", IsActive = true });
            var uow = new Mock<IUnitOfWork>();
            uow.SetupGet(u => u.OrganizationUnits).Returns(repo.Object);
            var provider = BuildProvider(uow);
            var logger = Mock.Of<ILogger<TenantContext>>();
            var context = new TenantContext(logger, provider);

            var result = await context.ResolveTenantFromSlugAsync("slug");

            Assert.True(result);
            Assert.True(context.HasTenant);
            Assert.Equal(tenantId, context.CurrentTenantId);
        }

        [Fact]
        public async Task ResolveTenantFromSlugAsync_InvalidSlug_ReturnsFalse()
        {
            var repo = new Mock<IRepository<OrganizationUnit>>();
            repo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<OrganizationUnit, bool>>>(), null))
                .ReturnsAsync((OrganizationUnit?)null);
            var uow = new Mock<IUnitOfWork>();
            uow.SetupGet(u => u.OrganizationUnits).Returns(repo.Object);
            var provider = BuildProvider(uow);
            var logger = Mock.Of<ILogger<TenantContext>>();
            var context = new TenantContext(logger, provider);

            var result = await context.ResolveTenantFromSlugAsync("missing");

            Assert.False(result);
            Assert.False(context.HasTenant);
        }
    }
}
