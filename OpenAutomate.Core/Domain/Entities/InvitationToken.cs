using OpenAutomate.Core.Domain.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class InvitationToken : BaseEntity
    {
        [Required]
        public Guid OrganizationUnitId { get; set; }
        
        [ForeignKey("OrganizationUnitId")]
        public virtual OrganizationUnit OrganizationUnit { get; set; }
        
        [Required]
        public Guid InviterId { get; set; }
        
        [ForeignKey("InviterId")]
        public virtual User Inviter { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        public string Name { get; set; }
        
        [Required]
        public string Token { get; set; }
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        public Guid? AcceptedByUserId { get; set; }
        
        [ForeignKey("AcceptedByUserId")]
        public virtual User AcceptedByUser { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
} 