using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class OrganizationUnitUserConfiguration : IEntityTypeConfiguration<OrganizationUnitUser>
    {
        public void Configure(EntityTypeBuilder<OrganizationUnitUser> builder)
        {
            builder.ToTable("OrganizationUnitUsers");
            
            // Composite key
            builder.HasKey(ouu => new { ouu.UserId, ouu.OrganizationUnitId });
            
            // Setup relationships
            builder.HasOne(ouu => ouu.User)
                .WithMany(u => u.OrganizationUnitUsers)
                .HasForeignKey(ouu => ouu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(ouu => ouu.OrganizationUnit)
                .WithMany(ou => ou.OrganizationUnitUsers)
                .HasForeignKey(ouu => ouu.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 