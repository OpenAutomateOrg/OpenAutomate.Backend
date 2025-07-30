namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Settings for Redis configuration
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection string (host:port)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Instance name for distributed cache
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Database number to use (default: 0)
    /// </summary>
    public int Database { get; set; } = 0;
    
    /// <summary>
    /// Whether to abort on connection failure (default: false for resilience)
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;
    
    /// <summary>
    /// Redis username for authentication (leave empty if not using authentication)
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Redis password for authentication (leave empty if not using authentication)
    /// </summary>
    public string Password { get; set; } = string.Empty;
} 