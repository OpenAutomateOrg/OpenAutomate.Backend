using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class UserAuthorityConfiguration : IEntityTypeConfiguration<UserAuthority>
    {
        public void Configure(EntityTypeBuilder<UserAuthority> builder)
        {
            builder.ToTable("UserAuthorities");
            
            // Composite key to match existing DbContext configuration
            builder.HasKey(ua => new { ua.UserId, ua.AuthorityId });
            
            // Create index for faster lookups
            builder.HasIndex(ua => new { ua.UserId, ua.AuthorityId }).IsUnique();
            
            // Setup relationships
            builder.HasOne(ua => ua.User)
                .WithMany(u => u.Authorities)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(ua => ua.Authority)
                .WithMany(a => a.UserAuthorities)
                .HasForeignKey(ua => ua.AuthorityId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(ua => ua.OrganizationUnit)
                .WithMany()
                .HasForeignKey(ua => ua.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 