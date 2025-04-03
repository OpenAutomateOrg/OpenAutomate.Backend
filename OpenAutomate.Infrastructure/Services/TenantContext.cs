using System;
using Microsoft.AspNetCore.Http;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Services;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ITenantContext that retrieves the current tenant from HttpContext
    /// </summary>
    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// Gets the current tenant (organization) from HttpContext.Items
        /// </summary>
        public Organization CurrentTenant => 
            _httpContextAccessor.HttpContext?.Items["CurrentTenant"] as Organization;
        
        /// <summary>
        /// Gets the ID of the current tenant or Guid.Empty if no tenant
        /// </summary>
        public Guid CurrentTenantId => CurrentTenant?.Id ?? Guid.Empty;
        
        /// <summary>
        /// Indicates whether there is a current tenant context
        /// </summary>
        public bool HasTenant => CurrentTenant != null;
    }
} 