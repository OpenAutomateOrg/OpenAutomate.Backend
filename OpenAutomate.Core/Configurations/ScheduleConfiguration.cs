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
            
            // Setup relationships
            builder.HasOne(s => s.Package)
                .WithMany(ap => ap.Schedules)
                .HasForeignKey(s => s.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(s => s.Executions)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Create indexes for faster lookups
            builder.HasIndex(s => s.IsActive);
        }
    }
} 