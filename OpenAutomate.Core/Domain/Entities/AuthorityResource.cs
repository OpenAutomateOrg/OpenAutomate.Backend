using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class AuthorityResource : TenantEntity
    {
        [Required]
        public Guid AuthorityId { get; set; }
        
        [ForeignKey("AuthorityId")]
        public Authority Authority { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string ResourceName { get; set; } = string.Empty;
        
        /// <summary>
        /// Permission level (0-4): 0=No Access, 1=View, 2=Create, 3=Update (includes Execute), 4=Delete/FullAdmin
        /// Higher levels include all lower levels (hierarchical)
        /// </summary>
        [Required]
        [Range(0, 4)]
        public int Permission { get; set; }
        
        /// <summary>
        /// When this permission was granted
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this permission was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
} 