using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
    {
        public void Configure(EntityTypeBuilder<Execution> builder)
        {
            builder.ToTable("Executions");
            builder.HasKey(e => e.Id);
            
            builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            // Setup relationships
            builder.HasOne(e => e.BotAgent)
                .WithMany(ba => ba.Executions)
                .HasForeignKey(e => e.BotAgentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(e => e.Package)
                .WithMany(ap => ap.Executions)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Create indexes for faster lookups
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.StartTime);
        }
    }
} 