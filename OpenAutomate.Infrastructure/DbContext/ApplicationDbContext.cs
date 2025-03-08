using Microsoft.EntityFrameworkCore;
using OpenAutomate.Domain.Entity;

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

        private DbSet<User> Users { set; get; }
        private DbSet<OrganizationUnit> OrganizationUnits { set; get; }
        private DbSet<OrganizationUnitUser> OrganizationUnitUsers { set; get; }
        private DbSet<UserAuthority> UserAuthorities { set; get; }
        private DbSet<Authority> Authorities{ set; get; }





    }
}
