using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Decorator that adds caching capabilities to the TenantContext
/// </summary>
public class TenantContextCachingDecorator : ITenantContext
{
    private readonly ITenantContext _innerContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TenantContextCachingDecorator> _logger;

    // Cache TTL settings
    private static readonly TimeSpan TenantCacheTtl = TimeSpan.FromMinutes(30);

    // Log message templates
    private static class LogMessages
    {
        public const string TenantCacheHit = "Tenant cache hit for slug {TenantSlug}";
        public const string TenantCacheMiss = "Tenant cache miss for slug {TenantSlug}";
        public const string TenantCacheSet = "Tenant cached for slug {TenantSlug} with ID {TenantId}";
        public const string TenantCacheInvalidated = "Tenant cache invalidated for slug {TenantSlug}";
    }

    public TenantContextCachingDecorator(
        ITenantContext innerContext,
        ICacheService cacheService,
        ILogger<TenantContextCachingDecorator> logger)
    {
        _innerContext = innerContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region ITenantContext Implementation (Pass-through)

    public Guid CurrentTenantId => _innerContext.CurrentTenantId;

    public string? CurrentTenantSlug => _innerContext.CurrentTenantSlug;

    public bool HasTenant => _innerContext.HasTenant;

    public void SetTenant(Guid tenantId) => _innerContext.SetTenant(tenantId);

    public void SetTenant(Guid tenantId, string? tenantSlug) => _innerContext.SetTenant(tenantId, tenantSlug);

    public void ClearTenant() => _innerContext.ClearTenant();

    #endregion

    #region Cached Tenant Resolution

    public async Task<bool> ResolveTenantFromSlugAsync(string tenantSlug)
    {
        if (string.IsNullOrEmpty(tenantSlug))
        {
            return await _innerContext.ResolveTenantFromSlugAsync(tenantSlug);
        }

        var cacheKey = GetTenantCacheKey(tenantSlug);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<CachedTenantResult>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug(LogMessages.TenantCacheHit, tenantSlug);
            
            // Set the tenant in the inner context
            _innerContext.SetTenant(cachedResult.TenantId, tenantSlug);
            return true;
        }

        _logger.LogDebug(LogMessages.TenantCacheMiss, tenantSlug);

        // Get from database using inner context
        var result = await _innerContext.ResolveTenantFromSlugAsync(tenantSlug);
        
        // If successful, cache the result
        if (result && _innerContext.HasTenant)
        {
            var cacheValue = new CachedTenantResult 
            { 
                TenantId = _innerContext.CurrentTenantId,
                TenantSlug = tenantSlug 
            };
            
            await _cacheService.SetAsync(cacheKey, cacheValue, TenantCacheTtl);
            _logger.LogDebug(LogMessages.TenantCacheSet, tenantSlug, _innerContext.CurrentTenantId);
        }

        return result;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalidates the cache for a specific tenant slug
    /// This method can be called when a tenant's information is updated
    /// </summary>
    public async Task InvalidateTenantCacheAsync(string tenantSlug)
    {
        if (string.IsNullOrEmpty(tenantSlug)) return;

        var cacheKey = GetTenantCacheKey(tenantSlug);
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogDebug(LogMessages.TenantCacheInvalidated, tenantSlug);
    }

    /// <summary>
    /// Invalidates the cache for a tenant by ID
    /// This requires looking up the slug first, so it's less efficient
    /// </summary>
    public async Task InvalidateTenantCacheAsync(Guid tenantId)
    {
        // Note: This is a limitation of our simple caching approach
        // In a production system, you might want to maintain a reverse mapping
        // or use a more sophisticated cache invalidation strategy
        
        // For now, we'll let TTL handle this case
        _logger.LogDebug("Tenant cache invalidation by ID {TenantId} - relying on TTL", tenantId);
    }

    #endregion

    #region Cache Key Generation

    private static string GetTenantCacheKey(string tenantSlug)
    {
        return $"tenant:slug:{tenantSlug.ToLowerInvariant()}";
    }

    #endregion

    #region Cache DTOs

    private class CachedTenantResult
    {
        public Guid TenantId { get; set; }
        public string TenantSlug { get; set; } = string.Empty;
    }

    #endregion
} 