using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Configurations
{
    public class OrganizationUnitInvitationConfiguration : IEntityTypeConfiguration<OrganizationUnitInvitation>
    {
        public void Configure(EntityTypeBuilder<OrganizationUnitInvitation> builder)
        {
            builder.ToTable("OrganizationUnitInvitations");

            builder.HasKey(inv => inv.Id);

            builder.Property(inv => inv.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(inv => inv.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(inv => inv.ExpiresAt)
                .IsRequired();

            builder.HasOne(inv => inv.OrganizationUnit)
                .WithMany()
                .HasForeignKey(inv => inv.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(inv => inv.User)
                .WithMany()
                .HasForeignKey(inv => inv.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
