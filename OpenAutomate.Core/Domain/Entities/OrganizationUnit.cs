using System;
using System.Collections.Generic;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUnit : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // URL-friendly identifier for routing
        public bool IsActive { get; set; } = true;
        public DateTime? ScheduledDeletionAt { get; set; }  // When it will be deleted (null = not scheduled)
        public string? DeletionJobId { get; set; }          // Quartz job ID for cancellation

        // Navigation properties
        public virtual ICollection<OrganizationUnitUser> OrganizationUnitUsers { get; set; } = new List<OrganizationUnitUser>();
        public virtual ICollection<BotAgent> BotAgents { get; set; } = new List<BotAgent>();
        public virtual ICollection<AutomationPackage> AutomationPackages { get; set; } = new List<AutomationPackage>();
        public virtual ICollection<Execution> Executions { get; set; } = new List<Execution>();
    }
}
