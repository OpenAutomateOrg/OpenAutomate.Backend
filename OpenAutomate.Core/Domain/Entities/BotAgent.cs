using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class BotAgent : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        
        // Foreign key for User (Owner)
        public Guid OwnerId { get; set; }
        [JsonIgnore]
        public virtual User? Owner { get; set; }
        
        // Foreign key for OrganizationUnit
        public Guid OrganizationUnitId { get; set; }
        [JsonIgnore]
        public virtual OrganizationUnit? OrganizationUnit { get; set; }
        
        // Navigation property for executions
        [JsonIgnore]
        public virtual ICollection<Execution>? Executions { get; set; }
    }
} 