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
    
    // TTL Configuration Settings
    
    /// <summary>
    /// TTL for permission cache entries in minutes
    /// </summary>
    public int PermissionCacheTtlMinutes { get; set; } = 15;
    
    /// <summary>
    /// TTL for authority cache entries in minutes
    /// </summary>
    public int AuthorityCacheTtlMinutes { get; set; } = 15;
    
    /// <summary>
    /// TTL for tenant context cache entries in minutes
    /// </summary>
    public int TenantCacheTtlMinutes { get; set; } = 30;
    
    /// <summary>
    /// TTL for JWT blocklist entries in hours
    /// </summary>
    public int JwtBlocklistTtlHours { get; set; } = 24;
    
    /// <summary>
    /// Default TTL for API response cache entries in minutes
    /// </summary>
    public int ApiResponseCacheTtlMinutes { get; set; } = 5;
    
    /// <summary>
    /// TTL for health check cache entries in minutes
    /// </summary>
    public int HealthCheckCacheTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Enable or disable role-based caching (permissions and authorities)
    /// Set to false to disable caching for immediate permission updates
    /// </summary>
    public bool EnableRoleCaching { get; set; } = true;
    
    // Computed Properties for Easy Access
    
    /// <summary>
    /// Permission cache TTL as TimeSpan
    /// </summary>
    public TimeSpan PermissionCacheTtl => TimeSpan.FromMinutes(PermissionCacheTtlMinutes);
    
    /// <summary>
    /// Authority cache TTL as TimeSpan
    /// </summary>
    public TimeSpan AuthorityCacheTtl => TimeSpan.FromMinutes(AuthorityCacheTtlMinutes);
    
    /// <summary>
    /// Tenant cache TTL as TimeSpan
    /// </summary>
    public TimeSpan TenantCacheTtl => TimeSpan.FromMinutes(TenantCacheTtlMinutes);
    
    /// <summary>
    /// JWT blocklist TTL as TimeSpan
    /// </summary>
    public TimeSpan JwtBlocklistTtl => TimeSpan.FromHours(JwtBlocklistTtlHours);
    
    /// <summary>
    /// API response cache TTL as TimeSpan
    /// </summary>
    public TimeSpan ApiResponseCacheTtl => TimeSpan.FromMinutes(ApiResponseCacheTtlMinutes);
    
    /// <summary>
    /// Health check cache TTL as TimeSpan
    /// </summary>
    public TimeSpan HealthCheckCacheTtl => TimeSpan.FromMinutes(HealthCheckCacheTtlMinutes);
} 