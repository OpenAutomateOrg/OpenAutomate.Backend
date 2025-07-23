using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("Subscriptions");
            builder.HasKey(s => s.Id);

            // Properties
            builder.Property(s => s.LemonsqueezySubscriptionId)
                   .HasMaxLength(100);

            builder.Property(s => s.PlanName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(s => s.Status)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(s => s.TrialEndsAt)
                   .HasColumnType("datetime2");

            builder.Property(s => s.RenewsAt)
                   .HasColumnType("datetime2");

            builder.Property(s => s.EndsAt)
                   .HasColumnType("datetime2");

            // Indexes for performance
            builder.HasIndex(s => s.OrganizationUnitId);
            builder.HasIndex(s => s.LemonsqueezySubscriptionId);
            builder.HasIndex(s => s.Status);

            // Relationships
            builder.HasOne(s => s.OrganizationUnit)
                   .WithMany()
                   .HasForeignKey(s => s.OrganizationUnitId);
                   // Note: DeleteBehavior is set to NoAction globally in ApplicationDbContext
                   // to prevent cascade cycles in multi-tenant system
        }
    }
}