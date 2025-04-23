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
            
            // Override the default Id key from TenantEntity/BaseEntity with a composite key
            builder.HasKey(ouu => new { ouu.UserId, ouu.OrganizationUnitId });
            
            // Setup relationships
            builder.HasOne(ouu => ouu.User)
                .WithMany(u => u.OrganizationUnitUsers)
                .HasForeignKey(ouu => ouu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // This relationship is now handled by TenantEntity
            // but we still need to configure navigation and delete behavior
            builder.HasOne(ouu => ouu.OrganizationUnit)
                .WithMany(ou => ou.OrganizationUnitUsers)
                .HasForeignKey(ouu => ouu.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Role
            builder.Property(ouu => ouu.Role)
            .IsRequired()
            .HasMaxLength(50);
        }
    }
} 