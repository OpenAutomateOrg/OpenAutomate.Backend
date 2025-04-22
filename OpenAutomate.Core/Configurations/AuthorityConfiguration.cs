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
            
            // Define static GUIDs for seeding
            var adminId = new Guid("1a89f6f4-3c29-4fe1-9483-5de6676cc3f7");
            var userId = new Guid("7e4ea7df-5f1c-4234-8c7a-83d0c9ca2018");
            var operatorId = new Guid("e87a7aee-848a-46d4-b9f5-1e28c2571b3a");
            var developerId = new Guid("cfe55508-5a24-4f84-b436-36b1b4395436");
            
            // Use a static date for seeding
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Add initial seed data for predefined roles with static IDs and dates
            builder.HasData(
                new Authority { Id = adminId, Name = "ADMIN", CreatedAt = seedDate },
                new Authority { Id = userId, Name = "USER", CreatedAt = seedDate },
                new Authority { Id = operatorId, Name = "OPERATOR", CreatedAt = seedDate },
                new Authority { Id = developerId, Name = "DEVELOPER", CreatedAt = seedDate }
            );
        }
    }
} 