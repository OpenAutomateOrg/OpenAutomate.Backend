using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Base;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly ITenantContext _tenantContext;
        private readonly TenantQueryFilterService _tenantQueryFilterService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
            _tenantQueryFilterService = new TenantQueryFilterService(tenantContext);
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations
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
            modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionConfiguration());
            modelBuilder.ApplyConfiguration(new AssetConfiguration());
            modelBuilder.ApplyConfiguration(new AssetBotAgentConfiguration());
            
            // Configure all tenant entities to use NoAction for OrganizationUnit to prevent cascade cycles
            // This is important because each tenant entity inherits OrganizationUnitId from TenantEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Find all foreign keys in this entity that point to OrganizationUnit via the OrganizationUnitId property
                foreach (var foreignKey in entityType.GetForeignKeys()
                    .Where(fk => fk.PrincipalEntityType.ClrType == typeof(OrganizationUnit) && 
                                 fk.Properties.Count == 1 && 
                                 fk.Properties.First().Name == "OrganizationUnitId"))
                {
                    // Set the delete behavior to NoAction to prevent multiple cascade paths
                    foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
                }
            }
            
            // Apply tenant query filters to all tenant-aware entities
            _tenantQueryFilterService.ApplyTenantFilters(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            // Only set tenant ID for new entities when we have a tenant context
            if (_tenantContext.HasTenant)
            {
                // Find all added entities that implement ITenantEntity
                var tenantEntities = ChangeTracker.Entries<ITenantEntity>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity);

                // Set the tenant ID to the current tenant for all new tenant entities
                foreach (var entity in tenantEntities)
                {
                    if (entity.OrganizationUnitId == Guid.Empty)
                    {
                        entity.OrganizationUnitId = _tenantContext.CurrentTenantId;
                    }
                }
            }

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public DbSet<User> Users { set; get; }
        public DbSet<OrganizationUnit> OrganizationUnits { set; get; }
        public DbSet<OrganizationUnitUser> OrganizationUnitUsers { set; get; }
        public DbSet<UserAuthority> UserAuthorities { set; get; }
        public DbSet<Authority> Authorities{ set; get; }
        public DbSet<AuthorityResource> AuthorityResources { set; get; }
        public DbSet<RefreshToken> RefreshTokens { set; get; } 
        
        public DbSet<BotAgent> BotAgents { get; set; }
        public DbSet<AutomationPackage> AutomationPackages { get; set; }
        public DbSet<PackageVersion> PackageVersions { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetBotAgent> AssetBotAgents { get; set; }
    }
}
