using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class AssetConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.ToTable("Assets");
            
            builder.HasKey(a => a.Id);
              
            builder.Property(a => a.Key)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(a => a.Value)
                .IsRequired();
                
            builder.Property(a => a.Description)
                .HasMaxLength(500);
                
            // Create a unique index on Key within the same tenant
            builder.HasIndex(a => new { a.OrganizationUnitId, a.Key })
                .IsUnique();
                
            // Configure relationship with Organization Unit
            builder.HasOne(a => a.OrganizationUnit)
                .WithMany()
                .HasForeignKey(a => a.OrganizationUnitId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
} 