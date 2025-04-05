using System;
using OpenAutomate.Core.Domain.Interfaces;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ITenantContext that retrieves the current tenant from HttpContext
    /// </summary>
    public class TenantContext : ITenantContext
    {
        private Guid? _currentTenantId;

        public Guid CurrentTenantId 
        { 
            get 
            {
                if (!_currentTenantId.HasValue)
                {
                    throw new InvalidOperationException("No tenant has been set for the current context.");
                }
                return _currentTenantId.Value;
            }
        }

        public bool HasTenant => _currentTenantId.HasValue;

        public void SetTenant(Guid tenantId)
        {
            _currentTenantId = tenantId;
        }

        public void ClearTenant()
        {
            _currentTenantId = null;
        }
    }
} 