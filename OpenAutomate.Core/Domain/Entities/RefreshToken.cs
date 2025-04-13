using OpenAutomate.Core.Domain.Base;
using System;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;
        
        // Foreign key for User
        public Guid UserId { get; set; }
        [JsonIgnore]
        public virtual User? User { get; set; }
    }
} 