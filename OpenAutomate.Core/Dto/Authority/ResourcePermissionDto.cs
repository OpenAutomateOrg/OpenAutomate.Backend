using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Authority
{
    public class ResourcePermissionDto
    {
        public Guid? AuthorityId { get; set; }
        
        [Required]
        public string AuthorityName { get; set; } = string.Empty;
        
        [Required]
        public string ResourceName { get; set; } = string.Empty;
        
        [Required]
        [Range(0, 5)]
        public int Permission { get; set; }
        
        /// <summary>
        /// Human-readable permission description
        /// </summary>
        public string PermissionDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for the resource
        /// </summary>
        public string ResourceDisplayName { get; set; } = string.Empty;
    }
} 