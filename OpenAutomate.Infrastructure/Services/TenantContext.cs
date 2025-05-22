using System;
using OpenAutomate.Core.IServices;
using System.Threading;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ITenantContext that retrieves the current tenant from HttpContext
    /// </summary> 
    public class TenantContext : ITenantContext
    {
        private Guid? _currentTenantId;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

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
    }
} 