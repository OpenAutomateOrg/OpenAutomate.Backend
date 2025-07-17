namespace OpenAutomate.Core.IServices;

/// <summary>
/// Service for invalidating cache entries across multiple API instances using Redis Pub/Sub
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates a specific cache key across all instances
    /// </summary>
    /// <param name="cacheKey">The cache key to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheKeyAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates multiple cache keys across all instances
    /// </summary>
    /// <param name="cacheKeys">The cache keys to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheKeysAsync(IEnumerable<string> cacheKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cache keys matching a pattern across all instances
    /// </summary>
    /// <param name="pattern">The pattern to match (e.g., "perm:tenant123:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCachePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates user permissions cache for a specific user across all instances
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateUserPermissionsCacheAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all user permissions cache for a tenant across all instances
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateTenantPermissionsCacheAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates tenant resolution cache for a specific tenant across all instances
    /// </summary>
    /// <param name="tenantSlug">The tenant slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateTenantResolutionCacheAsync(string tenantSlug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates API response cache for specific patterns across all instances
    /// </summary>
    /// <param name="pathPattern">The API path pattern to invalidate</param>
    /// <param name="tenantId">Optional tenant ID to scope invalidation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateApiResponseCacheAsync(string pathPattern, Guid? tenantId = null, CancellationToken cancellationToken = default);
} 