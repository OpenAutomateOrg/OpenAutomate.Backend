using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Domain.Entities
{
    public class RefreshToken
    {
        [Key]
        public string Id { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        // using Lambda to check Revoked != null or not 
        public bool IsRevoked => Revoked != null;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
