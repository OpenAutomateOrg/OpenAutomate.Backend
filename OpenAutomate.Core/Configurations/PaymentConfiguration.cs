using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);

            // Properties
            builder.Property(p => p.LemonsqueezyOrderId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(p => p.LemonsqueezySubscriptionId)
                   .HasMaxLength(100);

            builder.Property(p => p.Amount)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                   .IsRequired()
                   .HasMaxLength(3);

            builder.Property(p => p.Status)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(p => p.PaymentDate)
                   .IsRequired()
                   .HasColumnType("datetime2");

            builder.Property(p => p.Description)
                   .HasMaxLength(500);

            builder.Property(p => p.CustomerEmail)
                   .HasMaxLength(255);

            // Indexes for performance
            builder.HasIndex(p => p.OrganizationUnitId);
            builder.HasIndex(p => p.LemonsqueezyOrderId).IsUnique();
            builder.HasIndex(p => p.LemonsqueezySubscriptionId);
            builder.HasIndex(p => p.PaymentDate);
            builder.HasIndex(p => p.Status);

            // Relationships
            builder.HasOne(p => p.OrganizationUnit)
                   .WithMany()
                   .HasForeignKey(p => p.OrganizationUnitId);
                   // Note: DeleteBehavior is set to NoAction globally in ApplicationDbContext
                   // to prevent cascade cycles in multi-tenant system
        }
    }
}