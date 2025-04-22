using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class AssetBotAgentConfiguration : IEntityTypeConfiguration<AssetBotAgent>
    {
        public void Configure(EntityTypeBuilder<AssetBotAgent> builder)
        {
            builder.ToTable("AssetBotAgents");
            
            builder.HasKey(aba => aba.Id);
            
            // Set up the relationship with Asset
            builder.HasOne(aba => aba.Asset)
                .WithMany(a => a.AssetBotAgents)
                .HasForeignKey(aba => aba.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Set up the relationship with BotAgent
            builder.HasOne(aba => aba.BotAgent)
                .WithMany(ba => ba.AssetBotAgents)
                .HasForeignKey(aba => aba.BotAgentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure OrganizationUnitId to be copied from the Asset on insert/update
            builder.Property(aba => aba.OrganizationUnitId)
                .IsRequired();
        }
    }
} 