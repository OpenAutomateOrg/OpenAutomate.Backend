using System;

namespace OpenAutomate.Core.Domain.BaseEntity
{
    /// <summary>
    /// Interface to mark entities that belong to a specific tenant (Organization)
    /// </summary>
    public interface BaseTenant
    {
        /// <summary>
        /// The ID of the organization (tenant) that owns this entity
        /// </summary>
        Guid OrganizationId { get; set; }
    }
}