using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System.Text.Json;
using StackExchange.Redis;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Redis-based implementation of ICacheService with graceful error handling
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedisCacheConfiguration _cacheConfig;

    // Log message templates
    private static class LogMessages
    {
        public const string CacheGetError = "Failed to retrieve cache key {CacheKey}";
        public const string CacheGetSuccess = "Successfully retrieved cache key {CacheKey}";
        public const string CacheSetError = "Failed to set cache key {CacheKey}";
        public const string CacheSetSuccess = "Successfully set cache key {CacheKey} with expiry {ExpiryMs}ms";
        public const string CacheRemoveError = "Failed to remove cache key {CacheKey}";
        public const string CacheRemoveSuccess = "Successfully removed cache key {CacheKey}";
        public const string CacheRemoveMultipleError = "Failed to remove multiple cache keys";
        public const string CacheRemoveMultipleSuccess = "Successfully removed {RemovedCount} cache keys";
        public const string CacheExistsError = "Failed to check existence of cache key {CacheKey}";
        public const string CacheRefreshError = "Failed to refresh cache key {CacheKey}";
        public const string CacheRefreshSuccess = "Successfully refreshed cache key {CacheKey} with expiry {ExpiryMs}ms";
        public const string CacheRemovePatternError = "Failed to remove cache keys matching pattern {Pattern}";
        public const string CacheRemovePatternSuccess = "Successfully removed {RemovedCount} cache keys matching pattern {Pattern}";
        public const string CacheRemovePatternStarted = "Started removing cache keys matching pattern {Pattern}";
        public const string CacheRemovePatternProgress = "Processed {ProcessedKeys} keys, removed {RemovedCount} keys for pattern {Pattern}";
        public const string SerializationError = "Failed to serialize object for cache key {CacheKey}";
        public const string DeserializationError = "Failed to deserialize object for cache key {CacheKey}";
    }

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger,
        IOptions<RedisCacheConfiguration> cacheConfig)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _cacheConfig = cacheConfig.Value;
        
        // Configure JSON options for consistent serialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            _logger.LogDebug(LogMessages.CacheGetSuccess, key);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, LogMessages.DeserializationError, key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheGetError, key);
            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            
            _logger.LogDebug(LogMessages.CacheSetSuccess, key, expiry?.TotalMilliseconds);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, LogMessages.SerializationError, key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheSetError, key);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug(LogMessages.CacheRemoveSuccess, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheRemoveError, key);
            return false;
        }
    }

    public async Task<long> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisKeys = keys.Select(key => (RedisKey)key).ToArray();
            
            var removedCount = await database.KeyDeleteAsync(redisKeys);
            _logger.LogDebug(LogMessages.CacheRemoveMultipleSuccess, removedCount);
            return removedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheRemoveMultipleError);
            return 0;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            return await database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheExistsError, key);
            return false;
        }
    }

    public async Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var result = await database.KeyExpireAsync(key, expiry);
            
            if (result)
            {
                _logger.LogDebug(LogMessages.CacheRefreshSuccess, key, expiry.TotalMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheRefreshError, key);
            return false;
        }
    }

    public async Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var endpoints = _connectionMultiplexer.GetEndPoints();
            if (endpoints == null || endpoints.Length == 0)
            {
                _logger.LogError("No Redis endpoints available for pattern removal. Pattern: {Pattern}", pattern);
                return 0;
            }
            var server = _connectionMultiplexer.GetServer(endpoints.First());
            var batchSize = _cacheConfig.BatchSize;
            var scanCount = _cacheConfig.ScanCount;
            var batchDelayMs = _cacheConfig.BatchDelayMs;
            var maxKeysPerPattern = _cacheConfig.MaxKeysPerPattern;

            var keys = new List<RedisKey>();
            long totalRemoved = 0;
            long totalProcessed = 0;

            _logger.LogInformation(LogMessages.CacheRemovePatternStarted, pattern);
            var keyEnumerator = server.Keys(database: database.Database, pattern: pattern, pageSize: scanCount);

            foreach (var key in keyEnumerator)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                keys.Add(key);
                totalProcessed++;

                if (totalProcessed >= maxKeysPerPattern)
                {
                    LogMaxKeyLimit(pattern, maxKeysPerPattern, totalProcessed, totalRemoved);
                    break;
                }

                if (keys.Count >= batchSize)
                {
                    totalRemoved += await DeleteBatchAsync(database, keys, cancellationToken);
                    keys.Clear();
                    LogProgressIfNeeded(totalProcessed, totalRemoved, pattern, batchSize);
                    await DelayIfNeeded(batchDelayMs, cancellationToken);
                }
            }

            if (keys.Count > 0)
            {
                totalRemoved += await DeleteBatchAsync(database, keys, cancellationToken);
            }

            _logger.LogDebug(LogMessages.CacheRemovePatternSuccess, totalRemoved, pattern);
            if (totalProcessed >= maxKeysPerPattern)
            {
                LogMaxKeyLimit(pattern, maxKeysPerPattern, totalProcessed, totalRemoved);
            }
            return totalRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.CacheRemovePatternError, pattern);
            return 0;
        }
    }

    private async Task<long> DeleteBatchAsync(IDatabase database, List<RedisKey> keys, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return 0;
        return await database.KeyDeleteAsync(keys.ToArray());
    }

    private void LogProgressIfNeeded(long totalProcessed, long totalRemoved, string pattern, int batchSize)
    {
        if (totalProcessed % (batchSize * 10) == 0)
        {
            _logger.LogDebug(LogMessages.CacheRemovePatternProgress, totalProcessed, totalRemoved, pattern);
        }
    }

    private async Task DelayIfNeeded(int batchDelayMs, CancellationToken cancellationToken)
    {
        if (batchDelayMs > 0)
        {
            await Task.Delay(batchDelayMs, cancellationToken);
        }
    }

    private void LogMaxKeyLimit(string pattern, int maxKeys, long processed, long removed)
    {
        _logger.LogWarning("Pattern removal stopped at maximum key limit. Pattern: {Pattern}, Processed: {ProcessedKeys}, Removed: {RemovedKeys}",
            pattern, processed, removed);
    }
} 