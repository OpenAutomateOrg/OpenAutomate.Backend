using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using StackExchange.Redis;
using System.Text.Json;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Implementation of cache invalidation service using Redis Pub/Sub
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly ISubscriber _subscriber;

    private static class LogMessages
    {
        public const string CacheKeyInvalidated = "Cache key invalidated: {CacheKey}";
        public const string CacheKeysInvalidated = "Cache keys invalidated: {CacheKeyCount}";
        public const string CachePatternInvalidated = "Cache pattern invalidated: {Pattern}";
        public const string InvalidationMessagePublished = "Cache invalidation message published to channel {Channel}";
        public const string InvalidationMessageReceived = "Cache invalidation message received from channel {Channel}";
        public const string InvalidationError = "Error during cache invalidation";
        public const string PublishError = "Error publishing cache invalidation message";
        public const string UserPermissionsCacheInvalidated = "User permissions cache invalidated for tenant {TenantId}, user {UserId}";
        public const string TenantPermissionsCacheInvalidated = "Tenant permissions cache invalidated for tenant {TenantId}";
        public const string TenantResolutionCacheInvalidated = "Tenant resolution cache invalidated for slug {TenantSlug}";
        public const string ApiResponseCacheInvalidated = "API response cache invalidated for pattern {PathPattern}";
    }

    public CacheInvalidationService(
        IConnectionMultiplexer redis,
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _redis = redis;
        _cacheService = cacheService;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
    }

    public async Task InvalidateCacheKeyAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new CacheInvalidationMessage
            {
                Type = CacheInvalidationType.Key,
                Keys = new[] { cacheKey },
                Timestamp = DateTimeOffset.UtcNow
            };

            await PublishInvalidationMessageAsync(message, cancellationToken);
            _logger.LogDebug(LogMessages.CacheKeyInvalidated, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateCacheKeysAsync(IEnumerable<string> cacheKeys, CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = cacheKeys.ToArray();
            var message = new CacheInvalidationMessage
            {
                Type = CacheInvalidationType.Keys,
                Keys = keys,
                Timestamp = DateTimeOffset.UtcNow
            };

            await PublishInvalidationMessageAsync(message, cancellationToken);
            _logger.LogDebug(LogMessages.CacheKeysInvalidated, keys.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateCachePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new CacheInvalidationMessage
            {
                Type = CacheInvalidationType.Pattern,
                Pattern = pattern,
                Timestamp = DateTimeOffset.UtcNow
            };

            await PublishInvalidationMessageAsync(message, cancellationToken);
            _logger.LogDebug(LogMessages.CachePatternInvalidated, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateUserPermissionsCacheAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = $"perm:{tenantId}:{userId}:*";
            await InvalidateCachePatternAsync(pattern, cancellationToken);
            _logger.LogInformation(LogMessages.UserPermissionsCacheInvalidated, tenantId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateTenantPermissionsCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = $"perm:{tenantId}:*";
            await InvalidateCachePatternAsync(pattern, cancellationToken);
            _logger.LogInformation(LogMessages.TenantPermissionsCacheInvalidated, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateTenantResolutionCacheAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"tenant:slug:{tenantSlug.ToLowerInvariant()}";
            await InvalidateCacheKeyAsync(cacheKey, cancellationToken);
            _logger.LogInformation(LogMessages.TenantResolutionCacheInvalidated, tenantSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    public async Task InvalidateApiResponseCacheAsync(string pathPattern, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = tenantId.HasValue 
                ? $"api-cache:*{pathPattern}*tenant:{tenantId}*"
                : $"api-cache:*{pathPattern}*";
            
            await InvalidateCachePatternAsync(pattern, cancellationToken);
            _logger.LogInformation(LogMessages.ApiResponseCacheInvalidated, pathPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }

    private async Task PublishInvalidationMessageAsync(CacheInvalidationMessage message, CancellationToken cancellationToken)
    {
        try
        {
            const string channel = "cache:invalidate";
            var serializedMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _subscriber.PublishAsync(channel, serializedMessage);
            _logger.LogDebug(LogMessages.InvalidationMessagePublished, channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.PublishError);
            throw;
        }
    }

    /// <summary>
    /// Processes received cache invalidation messages (called by the background service)
    /// </summary>
    /// <param name="message">The invalidation message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ProcessInvalidationMessageAsync(CacheInvalidationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (message.Type)
            {
                case CacheInvalidationType.Key:
                    if (message.Keys?.Length > 0)
                    {
                        await _cacheService.RemoveAsync(message.Keys[0]);
                    }
                    break;

                case CacheInvalidationType.Keys:
                    if (message.Keys?.Length > 0)
                    {
                        foreach (var key in message.Keys)
                        {
                            await _cacheService.RemoveAsync(key);
                        }
                    }
                    break;

                case CacheInvalidationType.Pattern:
                    if (!string.IsNullOrEmpty(message.Pattern))
                    {
                        await RemoveKeysByPatternAsync(message.Pattern);
                    }
                    break;
            }

            _logger.LogDebug(LogMessages.InvalidationMessageReceived, "cache:invalidate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            // Don't rethrow - we don't want to crash the background service
        }
    }

    private async Task RemoveKeysByPatternAsync(string pattern)
    {
        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            // Use SCAN to find keys matching the pattern
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                await database.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern {Pattern}", pattern);
            throw;
        }
    }
}

/// <summary>
/// Message format for cache invalidation
/// </summary>
public class CacheInvalidationMessage
{
    public CacheInvalidationType Type { get; set; }
    public string[]? Keys { get; set; }
    public string? Pattern { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Types of cache invalidation
/// </summary>
public enum CacheInvalidationType
{
    Key,
    Keys,
    Pattern
} 