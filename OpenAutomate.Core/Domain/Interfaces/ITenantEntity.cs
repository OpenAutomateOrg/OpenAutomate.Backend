using System;

namespace OpenAutomate.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface to mark entities that belong to a specific tenant (Organization)
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// The ID of the organization (tenant) that owns this entity
        /// </summary>
        Guid OrganizationId { get; set; }
    }
} 