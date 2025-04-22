using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            
            // Setup relationships
            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany<BotAgent>()
                .WithOne(ba => ba.Owner)
                .HasForeignKey(ba => ba.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany<AutomationPackage>()
                .WithOne(ap => ap.Creator)
                .HasForeignKey(ap => ap.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany<Schedule>()
                .WithOne(s => s.CreatedBy)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 