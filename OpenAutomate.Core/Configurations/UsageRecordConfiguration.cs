using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
    {
        public void Configure(EntityTypeBuilder<UsageRecord> builder)
        {
            builder.ToTable("UsageRecords");
            builder.HasKey(u => u.Id);

            // Properties
            builder.Property(u => u.Feature)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(u => u.UsageCount)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(u => u.ResetDate)
                   .IsRequired()
                   .HasColumnType("datetime2");

            builder.Property(u => u.UsageLimit)
                   .IsRequired(false);

            // Indexes for performance
            builder.HasIndex(u => u.OrganizationUnitId);
            builder.HasIndex(u => new { u.OrganizationUnitId, u.Feature }).IsUnique();
            builder.HasIndex(u => u.ResetDate);

            // Relationships
            builder.HasOne(u => u.OrganizationUnit)
                   .WithMany()
                   .HasForeignKey(u => u.OrganizationUnitId);
                   // Note: DeleteBehavior is set to NoAction globally in ApplicationDbContext
                   // to prevent cascade cycles in multi-tenant system
        }
    }
}