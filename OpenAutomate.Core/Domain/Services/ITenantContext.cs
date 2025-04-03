using System;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.Services
{
    /// <summary>
    /// Interface for accessing the current tenant context
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>
        /// Gets the current tenant (organization)
        /// </summary>
        Organization CurrentTenant { get; }
        
        /// <summary>
        /// Gets the ID of the current tenant (organization)
        /// </summary>
        Guid CurrentTenantId { get; }
        
        /// <summary>
        /// Indicates whether there is a current tenant context
        /// </summary>
        bool HasTenant { get; }
    }
} 