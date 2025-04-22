using System;

namespace OpenAutomate.Core.Domain.Base
{
    /// <summary>
    /// Interface for entities that belong to a specific organization unit (tenant)
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// Gets or sets the organization unit ID (tenant ID) this entity belongs to
        /// </summary>
        Guid OrganizationUnitId { get; set; }
    }
}