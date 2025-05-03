using OpenAutomate.Core.Domain.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class EmailVerificationToken : BaseEntity
    {
        // Default constructor for EF Core
        public EmailVerificationToken()
        {
            Token = string.Empty;
        }
        
        [Required]
        public Guid UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [Required]
        public string Token { get; set; } = "";
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
        public bool IsActive => !IsUsed && !IsExpired;
    }
} 