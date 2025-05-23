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

        [JsonIgnore]
        public virtual User? Creator { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual ICollection<PackageVersion> Versions { get; set; } = new List<PackageVersion>();

        [JsonIgnore]
        public virtual ICollection<Execution> Executions { get; set; } = new List<Execution>();

        [JsonIgnore]
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}