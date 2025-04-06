using System;

namespace OpenAutomate.Core.IServices
{
    public interface ITenantContext
    {
        /// <summary>
        /// Gets the current tenant's ID
        /// </summary>
        Guid CurrentTenantId { get; }

        /// <summary>
        /// Gets a value indicating whether a tenant is available in the current context
        /// </summary>
        bool HasTenant { get; }

        /// <summary>
        /// Sets the current tenant context
        /// </summary>
        /// <param name="tenantId">The tenant ID to set</param>
        void SetTenant(Guid tenantId);

        /// <summary>
        /// Clears the current tenant context
        /// </summary>
        void ClearTenant();
    }
}