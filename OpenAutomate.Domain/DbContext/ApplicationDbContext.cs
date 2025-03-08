using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAutomate.Domain.Entity;

namespace OpenAutomate.Domain.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
          
        }

        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     // Thực hiện seeding dữ liệu trong phương thức này
        //     modelBuilder.Entity<User>().HasData(
        //         new User { Id = Guid.NewGuid().ToString() , TenSanPham = "Sản phẩm mẫu", Gia = 100 }
        //     );
        // }

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
