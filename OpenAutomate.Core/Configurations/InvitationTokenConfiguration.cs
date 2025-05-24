using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class InvitationTokenConfiguration : IEntityTypeConfiguration<InvitationToken>
    {
        public void Configure(EntityTypeBuilder<InvitationToken> builder)
        {
            // Primary key
            builder.HasKey(e => e.Id);

            // Properties
            builder.Property(e => e.Email)
                .IsRequired();
                
            builder.Property(e => e.Name)
                .IsRequired(false);

            builder.Property(e => e.Token)
                .IsRequired();

            builder.Property(e => e.ExpiresAt)
                .IsRequired();

            builder.Property(e => e.IsUsed)
                .HasDefaultValue(false);

            builder.Property(e => e.UsedAt)
                .IsRequired(false);
                
            builder.Property(e => e.AcceptedByUserId)
                .IsRequired(false);

            // Relationships
            builder.HasOne(e => e.OrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(e => e.Inviter)
                .WithMany()
                .HasForeignKey(e => e.InviterId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(e => e.AcceptedByUser)
                .WithMany()
                .HasForeignKey(e => e.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table name
            builder.ToTable("InvitationTokens");
        }
    }
} 