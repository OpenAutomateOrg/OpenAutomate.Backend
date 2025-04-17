using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;

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
            
            // Apply tenant query filters to all tenant-aware entities
            _tenantQueryFilterService.ApplyTenantFilters(modelBuilder);
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
