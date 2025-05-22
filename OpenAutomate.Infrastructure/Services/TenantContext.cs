using System;
using OpenAutomate.Core.IServices;
using System.Threading;
using System.Threading.Tasks;
using OpenAutomate.Core.Domain.IRepository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ITenantContext that retrieves the current tenant from HttpContext
    /// </summary> 
    public class TenantContext : ITenantContext
    {
        private Guid? _currentTenantId;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ILogger<TenantContext> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TenantContext(ILogger<TenantContext> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Guid CurrentTenantId 
        { 
            get 
            {
                try
                {
                    _lock.EnterReadLock();
                    if (!_currentTenantId.HasValue)
                    {
                        throw new InvalidOperationException("No tenant has been set for the current context.");
                    }
                    return _currentTenantId.Value;
                }
                finally
                {
                    if (_lock.IsReadLockHeld)
                    {
                        _lock.ExitReadLock();
                    }
                }
            }
        }

        public bool HasTenant 
        { 
            get 
            {
                try
                {
                    _lock.EnterReadLock();
                    return _currentTenantId.HasValue;
                }
                finally
                {
                    if (_lock.IsReadLockHeld)
                    {
                        _lock.ExitReadLock();
                    }
                }
            }
        }

        public void SetTenant(Guid tenantId)
        {
            try
            {
                _lock.EnterWriteLock();
                _currentTenantId = tenantId;
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        public void ClearTenant()
        {
            try
            {
                _lock.EnterWriteLock();
                _currentTenantId = null;
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        public async Task<bool> ResolveTenantFromSlugAsync(string tenantSlug)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantSlug))
                {
                    _logger.LogError("Cannot resolve tenant: tenant slug is null or empty");
                    return false;
                }
                
                _logger.LogInformation("Attempting to resolve tenant from slug: {TenantSlug}", tenantSlug);
                
                // Use a scoped service to avoid circular dependency
                using (var scope = _serviceProvider.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    
                    var tenant = await unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == tenantSlug && o.IsActive);
                        
                    if (tenant == null)
                    {
                        _logger.LogWarning("Tenant not found for slug: {TenantSlug}", tenantSlug);
                        return false;
                    }
                    
                    // Set the tenant ID in the tenant context
                    SetTenant(tenant.Id);
                    
                    _logger.LogInformation("Tenant resolved successfully: {TenantId}, {TenantName}", tenant.Id, tenant.Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant from slug {TenantSlug}: {Message}", tenantSlug, ex.Message);
                return false;
            }
        }
    }
} 