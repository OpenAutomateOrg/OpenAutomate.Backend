using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class AutomationPackage : BaseEntity.BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Foreign key for User (Creator)
        public Guid CreatorId { get; set; }
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