using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class AuthorityResourceConfiguration : IEntityTypeConfiguration<AuthorityResource>
    {
        public void Configure(EntityTypeBuilder<AuthorityResource> builder)
        {
            builder.ToTable("AuthorityResources");
            builder.HasKey(ar => ar.Id);
            builder.Property(ar => ar.ResourceName).IsRequired().HasMaxLength(50);
            builder.Property(ar => ar.Permission).IsRequired();
            
            // Create index for faster lookups
            builder.HasIndex(ar => new { ar.AuthorityId, ar.ResourceName });
            
            // Setup relationships
            builder.HasOne(ar => ar.Authority)
                .WithMany(a => a.AuthorityResources)
                .HasForeignKey(ar => ar.AuthorityId)
                .OnDelete(DeleteBehavior.Restrict);
                
        }
    }
} 