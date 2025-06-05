using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Authority
{
    public class CreateAuthorityDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// List of resource permissions to assign to this authority
        /// </summary>
        public List<CreateResourcePermissionDto> ResourcePermissions { get; set; } = new();
    }
    
    public class CreateResourcePermissionDto
    {
        [Required]
        public string ResourceName { get; set; } = string.Empty;
        
        [Range(0, 5)]
        public int Permission { get; set; }
    }
    
    public class UpdateAuthorityDto
    {
        [StringLength(50, MinimumLength = 2)]
        public string? Name { get; set; }
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        /// <summary>
        /// List of resource permissions to replace existing permissions
        /// If null, permissions are not updated
        /// </summary>
        public List<CreateResourcePermissionDto>? ResourcePermissions { get; set; }
    }
    
    public class AvailableResourceDto
    {
        public string ResourceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<PermissionLevelDto> AvailablePermissions { get; set; } = new();
    }
    
    public class PermissionLevelDto
    {
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
} 