using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Decorator that adds caching capabilities to the AuthorizationManager
/// </summary>
public class AuthorizationManagerCachingDecorator : IAuthorizationManager
{
    private readonly IAuthorizationManager _innerManager;
    private readonly ICacheService _cacheService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuthorizationManagerCachingDecorator> _logger;

    // Cache TTL settings
    private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AuthorityCacheTtl = TimeSpan.FromMinutes(15);

    // Log message templates
    private static class LogMessages
    {
        public const string PermissionCacheHit = "Permission cache hit for user {UserId}, resource {ResourceName}, permission {Permission}";
        public const string PermissionCacheMiss = "Permission cache miss for user {UserId}, resource {ResourceName}, permission {Permission}";
        public const string AuthorityCacheHit = "Authority cache hit for user {UserId}, authority {AuthorityName}";
        public const string AuthorityCacheMiss = "Authority cache miss for user {UserId}, authority {AuthorityName}";
        public const string CacheInvalidated = "Cache invalidated for key pattern {KeyPattern}";
    }

    public AuthorizationManagerCachingDecorator(
        IAuthorizationManager innerManager,
        ICacheService cacheService,
        ITenantContext tenantContext,
        ILogger<AuthorizationManagerCachingDecorator> logger)
    {
        _innerManager = innerManager;
        _cacheService = cacheService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    #region Permission Checking (Cached)

    public async Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission)
    {
        if (!_tenantContext.HasTenant)
        {
            return await _innerManager.HasPermissionAsync(userId, resourceName, permission);
        }

        var cacheKey = GetPermissionCacheKey(_tenantContext.CurrentTenantId, userId, resourceName, permission);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<CachedPermissionResult>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug(LogMessages.PermissionCacheHit, userId, resourceName, permission);
            return cachedResult.HasPermission;
        }

        _logger.LogDebug(LogMessages.PermissionCacheMiss, userId, resourceName, permission);

        // Get from database and cache the result
        var result = await _innerManager.HasPermissionAsync(userId, resourceName, permission);
        
        var cacheValue = new CachedPermissionResult { HasPermission = result };
        await _cacheService.SetAsync(cacheKey, cacheValue, PermissionCacheTtl);

        return result;
    }

    public async Task<bool> HasAuthorityAsync(Guid userId, string authorityName)
    {
        if (!_tenantContext.HasTenant)
        {
            return await _innerManager.HasAuthorityAsync(userId, authorityName);
        }

        var cacheKey = GetAuthorityCacheKey(_tenantContext.CurrentTenantId, userId, authorityName);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<CachedAuthorityResult>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug(LogMessages.AuthorityCacheHit, userId, authorityName);
            return cachedResult.HasAuthority;
        }

        _logger.LogDebug(LogMessages.AuthorityCacheMiss, userId, authorityName);

        // Get from database and cache the result
        var result = await _innerManager.HasAuthorityAsync(userId, authorityName);
        
        var cacheValue = new CachedAuthorityResult { HasAuthority = result };
        await _cacheService.SetAsync(cacheKey, cacheValue, AuthorityCacheTtl);

        return result;
    }

    #endregion

    #region Authority Management (With Cache Invalidation)

    public async Task<AuthorityWithPermissionsDto> CreateAuthorityAsync(CreateAuthorityDto dto)
    {
        var result = await _innerManager.CreateAuthorityAsync(dto);
        await InvalidateAuthorityCaches();
        return result;
    }

    public async Task<AuthorityWithPermissionsDto?> GetAuthorityWithPermissionsAsync(Guid authorityId)
    {
        return await _innerManager.GetAuthorityWithPermissionsAsync(authorityId);
    }

    public async Task<IEnumerable<AuthorityWithPermissionsDto>> GetAllAuthoritiesWithPermissionsAsync()
    {
        return await _innerManager.GetAllAuthoritiesWithPermissionsAsync();
    }

    public async Task UpdateAuthorityAsync(Guid authorityId, UpdateAuthorityDto dto)
    {
        await _innerManager.UpdateAuthorityAsync(authorityId, dto);
        await InvalidateAuthorityCaches();
    }

    public async Task DeleteAuthorityAsync(Guid authorityId)
    {
        await _innerManager.DeleteAuthorityAsync(authorityId);
        await InvalidateAuthorityCaches();
    }

    #endregion

    #region User-Authority Assignments (With Cache Invalidation)

    public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
    {
        return await _innerManager.GetUserAuthoritiesAsync(userId);
    }

    public async Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId)
    {
        await _innerManager.AssignAuthorityToUserAsync(userId, authorityId);
        await InvalidateUserCaches(userId);
    }

    public async Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId)
    {
        await _innerManager.RemoveAuthorityFromUserAsync(userId, authorityId);
        await InvalidateUserCaches(userId);
    }

    public async Task AssignAuthorityToUserAsync(Guid userId, string authorityName)
    {
        await _innerManager.AssignAuthorityToUserAsync(userId, authorityName);
        await InvalidateUserCaches(userId);
    }

    public async Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName)
    {
        await _innerManager.RemoveAuthorityFromUserAsync(userId, authorityName);
        await InvalidateUserCaches(userId);
    }

    public async Task AssignAuthoritiesToUserAsync(Guid userId, List<Guid> authorityIds, Guid organizationUnitId)
    {
        await _innerManager.AssignAuthoritiesToUserAsync(userId, authorityIds, organizationUnitId);
        await InvalidateUserCaches(userId);
    }

    #endregion

    #region Resource Permissions (With Cache Invalidation)

    public async Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission)
    {
        await _innerManager.AddResourcePermissionAsync(authorityName, resourceName, permission);
        await InvalidateAuthorityCaches();
    }

    public async Task RemoveResourcePermissionAsync(string authorityName, string resourceName)
    {
        await _innerManager.RemoveResourcePermissionAsync(authorityName, resourceName);
        await InvalidateAuthorityCaches();
    }

    #endregion

    #region Resource Information (Pass-through)

    public async Task<IEnumerable<AvailableResourceDto>> GetAvailableResourcesAsync()
    {
        return await _innerManager.GetAvailableResourcesAsync();
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalidates all permission and authority caches for a specific user
    /// </summary>
    private async Task InvalidateUserCaches(Guid userId)
    {
        if (!_tenantContext.HasTenant) return;

        var permissionPattern = GetPermissionCacheKeyPattern(_tenantContext.CurrentTenantId, userId);
        var authorityPattern = GetAuthorityCacheKeyPattern(_tenantContext.CurrentTenantId, userId);
        
        // Use Redis pattern-based invalidation to remove all matching keys
        try
        {
            await _cacheService.RemoveByPatternAsync($"{permissionPattern}*");
            await _cacheService.RemoveByPatternAsync($"{authorityPattern}*");
            
            _logger.LogDebug(LogMessages.CacheInvalidated, $"{permissionPattern}* and {authorityPattern}*");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate user caches for user {UserId}, falling back to TTL expiration", userId);
            // Fallback: TTL will handle cleanup if pattern removal fails
        }
    }

    /// <summary>
    /// Invalidates all authority-related caches
    /// </summary>
    private async Task InvalidateAuthorityCaches()
    {
        if (!_tenantContext.HasTenant) return;

        var pattern = GetTenantCacheKeyPattern(_tenantContext.CurrentTenantId);
        
        try
        {
            await _cacheService.RemoveByPatternAsync($"{pattern}*");
            _logger.LogDebug(LogMessages.CacheInvalidated, $"{pattern}*");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate authority caches for tenant {TenantId}, falling back to TTL expiration", _tenantContext.CurrentTenantId);
            // Fallback: TTL will handle cleanup if pattern removal fails
        }
    }

    #endregion

    #region Cache Key Generation

    private static string GetPermissionCacheKey(Guid tenantId, Guid userId, string resourceName, int permission)
    {
        return $"perm:{tenantId}:{userId}:{resourceName}:{permission}";
    }

    private static string GetAuthorityCacheKey(Guid tenantId, Guid userId, string authorityName)
    {
        return $"auth:{tenantId}:{userId}:{authorityName}";
    }

    private static string GetPermissionCacheKeyPattern(Guid tenantId, Guid userId)
    {
        return $"perm:{tenantId}:{userId}";
    }

    private static string GetAuthorityCacheKeyPattern(Guid tenantId, Guid userId)
    {
        return $"auth:{tenantId}:{userId}";
    }

    private static string GetTenantCacheKeyPattern(Guid tenantId)
    {
        return $"perm:{tenantId}";
    }

    #endregion

    #region Cache DTOs

    private class CachedPermissionResult
    {
        public bool HasPermission { get; set; }
    }

    private class CachedAuthorityResult
    {
        public bool HasAuthority { get; set; }
    }

    #endregion
} 