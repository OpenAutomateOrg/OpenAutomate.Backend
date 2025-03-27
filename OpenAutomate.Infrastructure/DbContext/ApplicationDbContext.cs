using Microsoft.EntityFrameworkCore;
using OpenAutomate.Domain.Entities;

namespace OpenAutomate.Infrastructure.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
          
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganizationUnitUser>()
           .HasKey(ouu => new { ouu.UserId, ouu.OrganizationUnitId });

            modelBuilder.Entity<UserAuthority>()
           .HasKey(ouu => new { ouu.UserId, ouu.AuthorityID });
        }

        public DbSet<User> Users { set; get; }
        public DbSet<OrganizationUnit> OrganizationUnits { set; get; }
        public DbSet<OrganizationUnitUser> OrganizationUnitUsers { set; get; }
        public DbSet<UserAuthority> UserAuthorities { set; get; }
        public DbSet<Authority> Authorities{ set; get; }
        public DbSet<RefreshToken> RefreshTokens { set; get; } 
        public DbSet<Robot> Robots { get; set; }




    }
}
