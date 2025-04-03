using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Infrastructure.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
          
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganizationUser>()
               .HasKey(ouu => new { ouu.UserId, ouu.OrganizationId });

            modelBuilder.Entity<UserAuthority>()
               .HasKey(ouu => new { ouu.UserId, ouu.AuthorityID });
               
            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<User>()
                .HasMany<BotAgent>()
                .WithOne(ba => ba.Owner)
                .HasForeignKey(ba => ba.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<User>()
                .HasMany<AutomationPackage>()
                .WithOne(ap => ap.Creator)
                .HasForeignKey(ap => ap.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<User>()
                .HasMany<Schedule>()
                .WithOne(s => s.CreatedBy)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<AutomationPackage>()
                .HasMany(ap => ap.Versions)
                .WithOne(pv => pv.Package)
                .HasForeignKey(pv => pv.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<AutomationPackage>()
                .HasMany(ap => ap.Schedules)
                .WithOne(s => s.Package)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<BotAgent>()
                .HasMany(ba => ba.Executions)
                .WithOne(e => e.BotAgent)
                .HasForeignKey(e => e.BotAgentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<AutomationPackage>()
                .HasMany(ap => ap.Executions)
                .WithOne(e => e.Package)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.Executions)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public DbSet<User> Users { set; get; }
        public DbSet<Organization> Organization { set; get; }
        public DbSet<OrganizationUser> OrganizationUsers { set; get; }
        public DbSet<UserAuthority> UserAuthorities { set; get; }
        public DbSet<Authority> Authorities{ set; get; }
        public DbSet<RefreshToken> RefreshTokens { set; get; } 
        
        public DbSet<BotAgent> BotAgents { get; set; }
        public DbSet<AutomationPackage> AutomationPackages { get; set; }
        public DbSet<PackageVersion> PackageVersions { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
    }
}
