using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Configurations
{
    public class AuthorityConfiguration : IEntityTypeConfiguration<Authority>
    {
        public void Configure(EntityTypeBuilder<Authority> builder)
        {
            builder.ToTable("Authorities");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(50);
            
            // Add initial seed data for predefined roles
            builder.HasData(
                new Authority { Id = Guid.NewGuid(), Name = "ADMIN", CreatedAt = DateTime.UtcNow },
                new Authority { Id = Guid.NewGuid(), Name = "USER", CreatedAt = DateTime.UtcNow },
                new Authority { Id = Guid.NewGuid(), Name = "OPERATOR", CreatedAt = DateTime.UtcNow },
                new Authority { Id = Guid.NewGuid(), Name = "DEVELOPER", CreatedAt = DateTime.UtcNow }
            );
        }
    }
} 