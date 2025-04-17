using OpenAutomate.Core.Domain.Base;
using System;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Base class for entities that belong to a specific organization unit (tenant)
    /// </summary>
    public abstract class TenantEntity : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Gets or sets the organization unit ID (tenant ID) this entity belongs to
        /// </summary>
        public Guid OrganizationUnitId { get; set; }
        
        /// <summary>
        /// Navigation property for the organization unit
        /// </summary>
        public virtual OrganizationUnit OrganizationUnit { get; set; }
    }
} 