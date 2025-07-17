using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenAutomate.Core.IServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenAutomate.API.Attributes;

/// <summary>
/// Attribute to enable response caching for API endpoints using Redis.
/// Caches the response based on request path, query parameters, and user context.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EnableResponseCacheAttribute : Attribute, IAsyncActionFilter
{
    private readonly int _durationInSeconds;
    private readonly bool _varyByUser;
    private readonly bool _varyByTenant;

    /// <summary>
    /// Initializes a new instance of the EnableResponseCacheAttribute.
    /// </summary>
    /// <param name="durationInSeconds">Cache duration in seconds</param>
    /// <param name="varyByUser">Whether to vary cache by user ID</param>
    /// <param name="varyByTenant">Whether to vary cache by tenant ID</param>
    public EnableResponseCacheAttribute(int durationInSeconds, bool varyByUser = false, bool varyByTenant = true)
    {
        _durationInSeconds = durationInSeconds;
        _varyByUser = varyByUser;
        _varyByTenant = varyByTenant;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cacheService = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<EnableResponseCacheAttribute>>();

        try
        {
            // Generate cache key based on request
            var cacheKey = GenerateCacheKey(context.HttpContext);

            // Try to get cached response
            var cachedResponse = await cacheService.GetAsync<CachedApiResponse>(cacheKey);
            if (cachedResponse != null)
            {
                logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                
                // Return cached response
                context.Result = new ContentResult
                {
                    Content = cachedResponse.Content,
                    ContentType = cachedResponse.ContentType,
                    StatusCode = cachedResponse.StatusCode
                };
                return;
            }

            logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

            // Execute the action
            var executedContext = await next();

            // Cache the response if it's successful
            if (executedContext.Result is ObjectResult objectResult && 
                objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
            {
                var responseToCache = new CachedApiResponse
                {
                    Content = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    }),
                    ContentType = "application/json",
                    StatusCode = objectResult.StatusCode ?? 200,
                    CachedAt = DateTimeOffset.UtcNow
                };

                await cacheService.SetAsync(cacheKey, responseToCache, TimeSpan.FromSeconds(_durationInSeconds));
                logger.LogDebug("Response cached with key: {CacheKey} for {Duration} seconds", cacheKey, _durationInSeconds);
            }
            else if (executedContext.Result is ContentResult contentResult &&
                     contentResult.StatusCode >= 200 && contentResult.StatusCode < 300)
            {
                var responseToCache = new CachedApiResponse
                {
                    Content = contentResult.Content ?? string.Empty,
                    ContentType = contentResult.ContentType ?? "text/plain",
                    StatusCode = contentResult.StatusCode ?? 200,
                    CachedAt = DateTimeOffset.UtcNow
                };

                await cacheService.SetAsync(cacheKey, responseToCache, TimeSpan.FromSeconds(_durationInSeconds));
                logger.LogDebug("Response cached with key: {CacheKey} for {Duration} seconds", cacheKey, _durationInSeconds);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error occurred during response caching. Continuing without cache.");
            
            // Continue execution if caching fails
            await next();
        }
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder();
        
        // Base path and method
        keyBuilder.Append($"resp:{context.Request.Method}:{context.Request.Path}");

        // Add query parameters (sorted for consistency)
        if (context.Request.Query.Any())
        {
            var sortedQuery = context.Request.Query
                .OrderBy(q => q.Key)
                .Select(q => $"{q.Key}={string.Join(",", q.Value)}")
                .ToList();
            keyBuilder.Append($"?{string.Join("&", sortedQuery)}");
        }

        // Add user context if required
        if (_varyByUser && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? 
                        context.User.FindFirst("nameid")?.Value ??
                        context.User.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                keyBuilder.Append($":user:{userId}");
            }
        }

        // Add tenant context if required
        if (_varyByTenant)
        {
            var tenantId = context.Items["TenantId"]?.ToString();
            if (!string.IsNullOrEmpty(tenantId))
            {
                keyBuilder.Append($":tenant:{tenantId}");
            }
        }

        // Hash the key to ensure it's not too long and is consistent
        var keyString = keyBuilder.ToString();
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        var hashedKey = Convert.ToHexString(hashedBytes).ToLowerInvariant();

        return $"api-cache:{hashedKey}";
    }

    /// <summary>
    /// Represents a cached API response
    /// </summary>
    public class CachedApiResponse
    {
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
        public int StatusCode { get; set; } = 200;
        public DateTimeOffset CachedAt { get; set; }
    }
} 