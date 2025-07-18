namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Configuration settings for Redis cache operations
/// </summary>
public class RedisCacheConfiguration
{
    /// <summary>
    /// Number of keys to process in a single batch during bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Number of keys to scan per SCAN iteration
    /// </summary>
    public int ScanCount { get; set; } = 1000;
    
    /// <summary>
    /// Delay in milliseconds between batch operations to prevent overwhelming Redis
    /// </summary>
    public int BatchDelayMs { get; set; } = 0;
    
    /// <summary>
    /// Maximum number of keys to process in a single pattern removal operation
    /// </summary>
    public int MaxKeysPerPattern { get; set; } = 10000;
} 