using System;
using System.Collections.Generic;

namespace OpenAutomate.Core.Dto.Authority
{
    public class AuthorityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ResourcePermissionDto>? Permissions { get; set; }
    }
    
    public class AuthorityWithPermissionsDto : AuthorityDto
    {
        public new List<ResourcePermissionDto> Permissions { get; set; } = new();
        public bool IsSystemAuthority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 