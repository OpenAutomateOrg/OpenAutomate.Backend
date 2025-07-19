using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Models;
using OpenAutomate.Core.Utilities;
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
    private readonly RedisCacheConfiguration _cacheConfig;

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
        public const string PatternScanStarted = "Started scanning for keys matching pattern {Pattern}";
        public const string PatternScanProgress = "Processed {ProcessedKeys} keys, removed {RemovedCount} keys for pattern {Pattern}";
        public const string PatternScanCompleted = "Completed scanning for pattern {Pattern}, removed {TotalRemoved} keys in {ElapsedMs}ms";
    }

    public CacheInvalidationService(
        IConnectionMultiplexer redis,
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger,
        IOptions<RedisCacheConfiguration> cacheConfig)
    {
        _redis = redis;
        _cacheService = cacheService;
        _logger = logger;
        _subscriber = _redis.GetSubscriber();
        _cacheConfig = cacheConfig.Value;
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
            // Since EnableResponseCacheAttribute uses hashed keys, we need to generate the possible cache keys
            // that would match the given path pattern and tenant. The cache key generation logic needs to 
            // mirror the logic in EnableResponseCacheAttribute.GenerateCacheKey()
            
            var keysToInvalidate = new List<string>();
            
            if (tenantId.HasValue)
            {
                // Generate cache keys for common request patterns that would match this path and tenant
                var commonPatterns = CacheKeyUtility.GenerateApiResponseKeyPatterns("GET", pathPattern, tenantId.Value.ToString());
                keysToInvalidate.AddRange(commonPatterns);
            }
            else
            {
                // For global invalidation, invalidate all api-cache keys
                await InvalidateCachePatternAsync("api-cache:*", cancellationToken);
                _logger.LogInformation(LogMessages.ApiResponseCacheInvalidated, pathPattern);
                return;
            }
            
            if (keysToInvalidate.Any())
            {
                // Use pattern matching to find and delete matching keys
                await InvalidateApiResponseCacheByPatternAsync(keysToInvalidate, cancellationToken);
            }
            
            _logger.LogInformation(LogMessages.ApiResponseCacheInvalidated, pathPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.InvalidationError);
            throw;
        }
    }



    private async Task InvalidateApiResponseCacheByPatternAsync(List<string> cacheKeys, CancellationToken cancellationToken)
    {
        try
        {
            var database = _redis.GetDatabase();
            var deletedCount = 0;
            
            foreach (var key in cacheKeys)
            {
                var deleted = await database.KeyDeleteAsync(key);
                if (deleted)
                {
                    deletedCount++;
                }
            }
            
            _logger.LogDebug("Deleted {DeletedCount} out of {TotalCount} API response cache keys", deletedCount, cacheKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating API response cache keys");
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
            var endpoints = _redis.GetEndPoints();
            if (endpoints == null || endpoints.Length == 0)
            {
                _logger.LogError("No Redis endpoints available for pattern removal. Pattern: {Pattern}", pattern);
                return;
            }
            var server = _redis.GetServer(endpoints.First());
            
            var batchSize = _cacheConfig.BatchSize;
            var scanCount = _cacheConfig.ScanCount;
            var batchDelayMs = _cacheConfig.BatchDelayMs;
            
            var keys = new List<RedisKey>();
            long totalRemoved = 0;
            long totalProcessed = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.LogInformation(LogMessages.PatternScanStarted, pattern);
            
            // Use server.Keys with pattern and pageSize for cursor-based iteration
            var keyEnumerator = server.Keys(database: database.Database, pattern: pattern, pageSize: scanCount);
            
            foreach (var key in keyEnumerator)
            {
                keys.Add(key);
                totalProcessed++;
                
                // Process in batches to avoid memory issues and reduce Redis blocking
                if (keys.Count >= batchSize)
                {
                    var removed = await database.KeyDeleteAsync(keys.ToArray());
                    totalRemoved += removed;
                    keys.Clear();
                    
                    // Log progress for large operations
                    if (totalProcessed % (batchSize * 10) == 0)
                    {
                        _logger.LogDebug(LogMessages.PatternScanProgress, totalProcessed, totalRemoved, pattern);
                    }
                    
                    // Add configurable delay to prevent overwhelming Redis
                    if (batchDelayMs > 0)
                    {
                        await Task.Delay(batchDelayMs);
                    }
                }
            }
            
            // Remove any remaining keys
            if (keys.Count > 0)
            {
                var removed = await database.KeyDeleteAsync(keys.ToArray());
                totalRemoved += removed;
            }
            
            stopwatch.Stop();
            _logger.LogInformation(LogMessages.PatternScanCompleted, pattern, totalRemoved, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern {Pattern}", pattern);
            throw;
        }
    }
} 