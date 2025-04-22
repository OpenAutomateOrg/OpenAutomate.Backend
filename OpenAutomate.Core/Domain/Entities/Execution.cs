using OpenAutomate.Core.Domain.Base;
using System;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Execution : TenantEntity
    {
        public Guid BotAgentId { get; set; }
        public Guid PackageId { get; set; }
        public Guid? ScheduleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? LogOutput { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual BotAgent? BotAgent { get; set; }
        
        [JsonIgnore]
        public virtual AutomationPackage? Package { get; set; }
        
        [JsonIgnore]
        public virtual Schedule? Schedule { get; set; }
    }
} 