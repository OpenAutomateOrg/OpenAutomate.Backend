using System.Security.Cryptography;
using System.Text;

namespace OpenAutomate.Core.Utilities;

/// <summary>
/// Centralized utility for cache key generation and hashing to ensure consistency across the system
/// </summary>
public static class CacheKeyUtility
{
    /// <summary>
    /// Cache key prefixes for different cache types
    /// </summary>
    public static class Prefixes
    {
        public const string Permission = "perm";
        public const string Authority = "auth";
        public const string Tenant = "tenant";
        public const string TenantSlug = "tenant:slug";
        public const string JwtBlocklist = "jwt-blocklist";
        public const string ApiResponse = "api-cache";
        public const string HealthCheck = "health";
    }
    
    /// <summary>
    /// Common cache key patterns for different operations
    /// </summary>
    public static class Patterns
    {
        public const string TenantPermissions = "perm:{0}*";
        public const string TenantAuthorities = "auth:{0}*";
        public const string TenantSlugPattern = "tenant:slug:*";
        public const string ApiResponsePattern = "api-cache:*";
        public const string JwtBlocklistPattern = "jwt-blocklist:*";
    }
    
    /// <summary>
    /// Generates a permission cache key
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="resource">Resource name</param>
    /// <returns>Formatted permission cache key</returns>
    public static string GeneratePermissionKey(string tenantId, string userId, string resource)
    {
        return $"{Prefixes.Permission}:{tenantId}:{userId}:{resource}";
    }
    
    /// <summary>
    /// Generates an authority cache key
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="userId">User identifier</param>
    /// <returns>Formatted authority cache key</returns>
    public static string GenerateAuthorityKey(string tenantId, string userId)
    {
        return $"{Prefixes.Authority}:{tenantId}:{userId}";
    }
    
    /// <summary>
    /// Generates a tenant cache key by tenant ID
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Formatted tenant cache key</returns>
    public static string GenerateTenantKey(string tenantId)
    {
        return $"{Prefixes.Tenant}:{tenantId}";
    }
    
    /// <summary>
    /// Generates a tenant cache key by slug
    /// </summary>
    /// <param name="tenantSlug">Tenant slug</param>
    /// <returns>Formatted tenant slug cache key</returns>
    public static string GenerateTenantSlugKey(string tenantSlug)
    {
        return $"{Prefixes.TenantSlug}:{tenantSlug.ToLowerInvariant()}";
    }
    
    /// <summary>
    /// Generates a JWT blocklist cache key
    /// </summary>
    /// <param name="jti">JWT ID claim</param>
    /// <returns>Formatted JWT blocklist cache key</returns>
    public static string GenerateJwtBlocklistKey(string jti)
    {
        return $"{Prefixes.JwtBlocklist}:{jti}";
    }
    
    /// <summary>
    /// Generates a user-level JWT blocklist cache key
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Formatted user JWT blocklist cache key</returns>
    public static string GenerateUserJwtBlocklistKey(string userId)
    {
        return $"{Prefixes.JwtBlocklist}:user:{userId}";
    }
    
    /// <summary>
    /// Generates a health check cache key
    /// </summary>
    /// <param name="checkName">Health check name</param>
    /// <returns>Formatted health check cache key</returns>
    public static string GenerateHealthCheckKey(string checkName)
    {
        return $"{Prefixes.HealthCheck}:{checkName}";
    }
    
    /// <summary>
    /// Generates an API response cache key with hashing for consistent length
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="path">Request path</param>
    /// <param name="queryString">Query string</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Hashed API response cache key</returns>
    public static string GenerateApiResponseKey(string method, string path, string? queryString = null, string? tenantId = null)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append($"{method.ToUpperInvariant()}:{path}");
        
        if (!string.IsNullOrEmpty(queryString))
        {
            keyBuilder.Append($"?{queryString}");
        }
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            keyBuilder.Append($":tenant:{tenantId}");
        }
        
        // Hash the key to ensure consistent length and avoid Redis key length issues
        var keyString = keyBuilder.ToString();
        var hashedKey = HashString(keyString);
        
        return $"{Prefixes.ApiResponse}:{hashedKey}";
    }
    
    /// <summary>
    /// Generates common API response cache key patterns for invalidation
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="basePath">Base request path</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of hashed cache key patterns</returns>
    public static List<string> GenerateApiResponseKeyPatterns(string method, string basePath, string tenantId)
    {
        var patterns = new List<string>();
        
        // Common query patterns that might be cached
        var commonQueryPatterns = new[]
        {
            "", // No query parameters
            "?$top=10&$skip=0",
            "?$top=20&$skip=0",
            "?$top=50&$skip=0",
            "?$orderby=Name%20asc",
            "?$orderby=Name%20desc",
            "?$orderby=CreatedAt%20desc",
            "?$orderby=UpdatedAt%20desc",
            "?$count=true",
            "?$top=10&$skip=0&$count=true",
            "?$top=20&$skip=0&$count=true",
            "?$top=50&$skip=0&$count=true"
        };
        
        foreach (var queryPattern in commonQueryPatterns)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"{method.ToUpperInvariant()}:{basePath}");
            
            if (!string.IsNullOrEmpty(queryPattern))
            {
                keyBuilder.Append(queryPattern);
            }
            
            keyBuilder.Append($":tenant:{tenantId}");
            
            // Hash the key to match GenerateApiResponseKey logic
            var keyString = keyBuilder.ToString();
            var hashedKey = HashString(keyString);
            
            patterns.Add($"{Prefixes.ApiResponse}:{hashedKey}");
        }
        
        return patterns;
    }
    
    /// <summary>
    /// Generates cache invalidation patterns for tenant-specific operations
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Dictionary of pattern types and their patterns</returns>
    public static Dictionary<string, string> GenerateTenantInvalidationPatterns(string tenantId)
    {
        return new Dictionary<string, string>
        {
            ["permissions"] = string.Format(Patterns.TenantPermissions, tenantId),
            ["authorities"] = string.Format(Patterns.TenantAuthorities, tenantId),
            ["tenant-slug"] = Patterns.TenantSlugPattern,
            ["api-responses"] = Patterns.ApiResponsePattern
        };
    }
    
    /// <summary>
    /// Hashes a string using SHA256 for consistent cache key generation
    /// </summary>
    /// <param name="input">String to hash</param>
    /// <returns>Lowercase hex representation of the hash</returns>
    private static string HashString(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }
} 