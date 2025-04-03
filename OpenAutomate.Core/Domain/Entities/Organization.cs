using System;
using System.Collections.Generic;
using OpenAutomate.Core.Domain.BaseEntity;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Organization : BaseEntity.BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; } // URL-friendly identifier for routing
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; }
        public virtual ICollection<BotAgent> BotAgents { get; set; }
        public virtual ICollection<AutomationPackage> AutomationPackages { get; set; }
        public virtual ICollection<Execution> Executions { get; set; }
        public virtual ICollection<Schedule> Schedules { get; set; }
    }
}
