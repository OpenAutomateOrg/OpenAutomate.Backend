using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Schedule : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ScheduleType Type { get; set; } = ScheduleType.Recurring;
        public DateTime? OneTimeExecutionDate { get; set; }

        // Foreign keys
        public Guid PackageId { get; set; }
        public Guid CreatedById { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual AutomationPackage? Package { get; set; }

        [JsonIgnore]
        public virtual User? User { get; set; }

        [JsonIgnore]
        public virtual ICollection<Execution>? Executions { get; set; }
    }
}