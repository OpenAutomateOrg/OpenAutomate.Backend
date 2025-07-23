using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
    {
        public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
        {
            builder.ToTable("OrganizationUnits");
            builder.HasKey(o => o.Id);
            
            builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
            builder.Property(o => o.Slug).IsRequired().HasMaxLength(100);
            builder.Property(o => o.Description).HasMaxLength(500);

            // Simple deletion fields
            builder.Property(o => o.ScheduledDeletionAt).IsRequired(false);
            builder.Property(o => o.DeletionJobId).HasMaxLength(100).IsRequired(false);

            // Create index for faster lookups
            builder.HasIndex(o => o.Slug).IsUnique();
            builder.HasIndex(o => o.ScheduledDeletionAt); // For finding pending deletions
        }
    }
} 