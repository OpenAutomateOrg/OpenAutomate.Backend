using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class BotAgentConfiguration : IEntityTypeConfiguration<BotAgent>
    {
        public void Configure(EntityTypeBuilder<BotAgent> builder)
        {
            builder.ToTable("BotAgents");
            builder.HasKey(ba => ba.Id);
            
            builder.Property(ba => ba.Name).IsRequired().HasMaxLength(100);
            builder.Property(ba => ba.Status).IsRequired();
            
            // Setup relationships
            builder.HasOne(ba => ba.Owner)
                .WithMany()
                .HasForeignKey(ba => ba.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(ba => ba.Executions)
                .WithOne(e => e.BotAgent)
                .HasForeignKey(e => e.BotAgentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Create indexes for faster lookups
            builder.HasIndex(ba => ba.Name);
            builder.HasIndex(ba => ba.Status);
        }
    }
} 