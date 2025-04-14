using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;

namespace OpenAutomate.Infrastructure.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly ITenantContext _tenantContext;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
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

            // Configure global query filters for multi-tenant entities
            ApplyGlobalQueryFilters(modelBuilder);
        }

        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // Skip query filters if no tenant context is established
            // This is important for system-wide operations and admin actions
            
            // Apply filter for OrganizationUnitUser to determine user's access to org units
            modelBuilder.Entity<OrganizationUnitUser>().HasQueryFilter(ouu => 
                !_tenantContext.HasTenant || 
                ouu.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
            // Apply filter for UserAuthority based on OrganizationUnitId
            modelBuilder.Entity<UserAuthority>().HasQueryFilter(ua => 
                !_tenantContext.HasTenant || 
                ua.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
            // Apply filter for AuthorityResource based on OrganizationUnitId
            modelBuilder.Entity<AuthorityResource>().HasQueryFilter(ar => 
                !_tenantContext.HasTenant || 
                ar.OrganizationUnitId == _tenantContext.CurrentTenantId);
            
            // Now we use the directly added OrganizationUnitId properties
            modelBuilder.Entity<BotAgent>().HasQueryFilter(ba => 
                !_tenantContext.HasTenant ||
                ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            modelBuilder.Entity<AutomationPackage>().HasQueryFilter(ap => 
                !_tenantContext.HasTenant ||
                ap.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            // For derived entities, we use the relationship with the parent entity
            // that has the OrganizationUnitId
            modelBuilder.Entity<PackageVersion>().HasQueryFilter(pv => 
                !_tenantContext.HasTenant || 
                pv.Package != null && 
                pv.Package.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            modelBuilder.Entity<Schedule>().HasQueryFilter(s => 
                !_tenantContext.HasTenant || 
                s.Package != null && 
                s.Package.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            modelBuilder.Entity<Execution>().HasQueryFilter(e => 
                !_tenantContext.HasTenant || 
                e.OrganizationUnitId == _tenantContext.CurrentTenantId);
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
    }
}
