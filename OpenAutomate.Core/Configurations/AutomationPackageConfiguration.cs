using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class AutomationPackageConfiguration : IEntityTypeConfiguration<AutomationPackage>
    {
        public void Configure(EntityTypeBuilder<AutomationPackage> builder)
        {
            builder.ToTable("AutomationPackages");
            builder.HasKey(ap => ap.Id);
            
            builder.Property(ap => ap.Name).IsRequired().HasMaxLength(100);
            
            // Setup relationships
            builder.HasOne(ap => ap.Creator)
                .WithMany()
                .HasForeignKey(ap => ap.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(ap => ap.OrganizationUnit)
                .WithMany(ou => ou.AutomationPackages)
                .HasForeignKey(ap => ap.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(ap => ap.Versions)
                .WithOne(pv => pv.Package)
                .HasForeignKey(pv => pv.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasMany(ap => ap.Schedules)
                .WithOne(s => s.Package)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasMany(ap => ap.Executions)
                .WithOne(e => e.Package)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Create indexes for faster lookups
            builder.HasIndex(ap => ap.Name);
            builder.HasIndex(ap => ap.OrganizationUnitId);
        }
    }
} 