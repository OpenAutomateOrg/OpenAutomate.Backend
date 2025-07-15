namespace OpenAutomate.Core.IServices;

/// <summary>
/// Provides distributed caching operations with graceful error handling
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache by key
    /// </summary>
    /// <typeparam name="T">Type of the cached object</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached object or null if not found or error occurred</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in the cache with an optional expiry time
    /// </summary>
    /// <typeparam name="T">Type of the object to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Object to cache</param>
    /// <param name="expiry">Optional expiry time (if null, uses Redis default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false if error occurred</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key was removed, false if key didn't exist or error occurred</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple keys from the cache
    /// </summary>
    /// <param name="keys">Cache keys to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys that were removed</returns>
    Task<long> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the expiry time of a cached item
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="expiry">New expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if expiry was updated, false if key doesn't exist or error occurred</returns>
    Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
} 