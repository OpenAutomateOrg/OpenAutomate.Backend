namespace OpenAutomate.Core.Models;

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