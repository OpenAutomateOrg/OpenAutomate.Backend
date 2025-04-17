using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class BotAgent : BaseEntity
    {
        public string MachineKey { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        
        public virtual User? Owner { get; set; }
        
        // Navigation property for executions
        [JsonIgnore]
        public virtual ICollection<Execution>? Executions { get; set; }
    }
} 