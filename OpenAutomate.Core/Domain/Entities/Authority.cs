using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Authority : TenantEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Indicates if this is a system-created authority that cannot be deleted
        /// </summary>
        public bool IsSystemAuthority { get; set; } = false;
        
        /// <summary>
        /// When the authority was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the authority was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual ICollection<UserAuthority>? UserAuthorities { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<AuthorityResource>? AuthorityResources { get; set; }
    }
}
