using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the Schedule entity
    /// </summary>
    public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
    {
        public void Configure(EntityTypeBuilder<Schedule> builder)
        {
            builder.ToTable("Schedules");
            builder.HasKey(s => s.Id);
            
            // Configure properties
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(s => s.Description)
                .HasMaxLength(500);
                
            builder.Property(s => s.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(s => s.RecurrenceType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(s => s.TimeZoneId)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("UTC");
                
            // Configure relationships
            builder.HasOne(s => s.AutomationPackage)
                .WithMany()
                .HasForeignKey(s => s.AutomationPackageId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(s => s.BotAgent)
                .WithMany()
                .HasForeignKey(s => s.BotAgentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Create indexes for better query performance
            builder.HasIndex(s => s.Name);
            builder.HasIndex(s => s.IsEnabled);
            builder.HasIndex(s => s.BotAgentId); // Non-unique index for query performance
            builder.HasIndex(s => s.AutomationPackageId);
            builder.HasIndex(s => s.OrganizationUnitId);
            
            // Note: Bot agents can have multiple schedules (many-to-one relationship)
            // No unique constraint on BotAgentId
        }
    }
} 