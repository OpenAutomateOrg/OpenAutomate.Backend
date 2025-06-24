using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Interface for accessing the current tenant context
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>
        /// Gets the current tenant ID
        /// </summary>
        Guid CurrentTenantId { get; }

        /// <summary>
        /// Gets the current tenant slug
        /// </summary>
        string? CurrentTenantSlug { get; }

        /// <summary>
        /// Gets a value indicating whether a tenant has been set
        /// </summary>
        bool HasTenant { get; }

        /// <summary>
        /// Sets the current tenant ID
        /// </summary>
        /// <param name="tenantId">The tenant ID to set</param>
        void SetTenant(Guid tenantId);

        /// <summary>
        /// Clears the current tenant
        /// </summary>
        void ClearTenant();

        /// <summary>
        /// Resolves tenant from slug and sets it in the tenant context
        /// </summary>
        /// <param name="tenantSlug">The tenant slug to resolve</param>
        /// <returns>True if tenant was resolved successfully, false otherwise</returns>
        Task<bool> ResolveTenantFromSlugAsync(string tenantSlug);
    }
}