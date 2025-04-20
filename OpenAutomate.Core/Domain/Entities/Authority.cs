using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Authority : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Navigation properties
        [JsonIgnore]
        public virtual ICollection<UserAuthority>? UserAuthorities { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<AuthorityResource>? AuthorityResources { get; set; }
    }
}
