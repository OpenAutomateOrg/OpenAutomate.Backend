using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Utilities;
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
        // Use the centralized cache key utility
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var queryString = context.Request.QueryString.Value?.TrimStart('?');
        var tenantId = _varyByTenant ? context.Items["TenantId"]?.ToString() : null;
        
        // Add user context to query string if required
        if (_varyByUser && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? 
                        context.User.FindFirst("nameid")?.Value ??
                        context.User.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                queryString = string.IsNullOrEmpty(queryString) 
                    ? $"user={userId}" 
                    : $"{queryString}&user={userId}";
            }
        }
            
        return CacheKeyUtility.GenerateApiResponseKey(method, path, queryString, tenantId);
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