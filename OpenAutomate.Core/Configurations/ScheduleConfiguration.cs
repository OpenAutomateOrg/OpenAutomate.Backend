using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
    {
        public void Configure(EntityTypeBuilder<Schedule> builder)
        {
            // Table name
            builder.ToTable("Schedules");

            // Primary key
            builder.HasKey(s => s.Id);
            
            // Properties
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.CronExpression)
                .HasMaxLength(100);

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.Type)
                .IsRequired()
                .HasConversion<int>();
            
            // Foreign key relationships
            builder.HasOne(s => s.Package)
                .WithMany(p => p.Schedules)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationships
            builder.HasMany(s => s.Executions)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes
            builder.HasIndex(s => s.Name);
            builder.HasIndex(s => s.IsActive);
            builder.HasIndex(s => s.Type);
            builder.HasIndex(s => s.CreatedById);
            builder.HasIndex(s => s.PackageId);
            builder.HasIndex(s => new { s.OrganizationUnitId, s.IsActive });
        }
    }
} 