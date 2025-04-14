using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
    {
        public void Configure(EntityTypeBuilder<Schedule> builder)
        {
            builder.ToTable("Schedules");
            builder.HasKey(s => s.Id);
            
            builder.Property(s => s.CronExpression).IsRequired().HasMaxLength(100);
            builder.Property(s => s.IsActive).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
            
            // Setup relationships
            builder.HasOne(s => s.Package)
                .WithMany(p => p.Schedules)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(s => s.CreatedBy)
                .WithMany(u => u.CreatedSchedules)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(s => s.OrganizationUnit)
                .WithMany(ou => ou.Schedules)
                .HasForeignKey(s => s.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(s => s.Executions)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create indexes for faster lookups
            builder.HasIndex(s => s.IsActive);
            builder.HasIndex(s => s.OrganizationUnitId);
        }
    }
} 