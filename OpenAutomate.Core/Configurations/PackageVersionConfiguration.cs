using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class PackageVersionConfiguration : IEntityTypeConfiguration<PackageVersion>
    {
        public void Configure(EntityTypeBuilder<PackageVersion> builder)
        {
            builder.ToTable("PackageVersions");
            builder.HasKey(pv => pv.Id);
            
            builder.Property(pv => pv.VersionNumber).IsRequired().HasMaxLength(50);
            
            // Setup relationships
            builder.HasOne(pv => pv.Package)
                .WithMany(ap => ap.Versions)
                .HasForeignKey(pv => pv.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create indexes for faster lookups
            builder.HasIndex(pv => new { pv.PackageId, pv.VersionNumber }).IsUnique();
        }
    }
} 