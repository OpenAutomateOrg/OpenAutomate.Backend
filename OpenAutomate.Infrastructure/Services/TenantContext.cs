using System;
using System.Linq;
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
        private string? _currentTenantSlug;
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

        public string? CurrentTenantSlug
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _currentTenantSlug;
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
                // Clear slug if setting tenant by ID only
                if (_currentTenantSlug == null)
                    _logger.LogDebug("Tenant set: {TenantId}", tenantId);
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        public void SetTenant(Guid tenantId, string? tenantSlug)
        {
            try
            {
                _lock.EnterWriteLock();
                _currentTenantId = tenantId;
                _currentTenantSlug = tenantSlug;
                _logger.LogDebug("Tenant set: {TenantId}, Slug: {TenantSlug}", tenantId, tenantSlug);
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
                _currentTenantSlug = null;
                _logger.LogDebug("Tenant cleared");
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
                
                // Get the current scoped UnitOfWork to avoid creating a new scope
                // This ensures we use the same DbContext instance that has the tenant filters
                var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
                
                // Temporarily bypass tenant filtering to resolve the tenant
                var tenants = await unitOfWork.OrganizationUnits
                    .GetAllIgnoringFiltersAsync(o => o.Slug == tenantSlug && o.IsActive);
                var tenant = tenants.FirstOrDefault();
                    
                if (tenant == null)
                {
                    _logger.LogWarning("Tenant not found for slug: {TenantSlug}", tenantSlug);
                    return false;
                }
                
                // Set the tenant ID and slug in the tenant context
                SetTenant(tenant.Id, tenantSlug);
                
                _logger.LogInformation("Tenant resolved successfully: {TenantId}, {TenantName}, Slug: {TenantSlug}", tenant.Id, tenant.Name, tenantSlug);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant from slug {TenantSlug}: {Message}", tenantSlug, ex.Message);
                return false;
            }
        }
    }
} 