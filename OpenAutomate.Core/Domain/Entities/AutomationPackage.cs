using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class AutomationPackage : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
       

        [JsonIgnore]
        public virtual User? Creator { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual ICollection<PackageVersion>? Versions { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<Execution>? Executions { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<Schedule>? Schedules { get; set; }
    }
} 