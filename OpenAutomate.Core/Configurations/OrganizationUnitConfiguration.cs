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
            
            // Create index for faster lookups
            builder.HasIndex(o => o.Slug).IsUnique();
        }
    }
} 