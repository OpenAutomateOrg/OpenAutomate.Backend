using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System.Threading;
using OpenAutomate.API.Extensions;

namespace OpenAutomate.API.Middleware
{
    /// <summary>
    /// Middleware that resolves the current tenant from the URL path
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;
        private static readonly SemaphoreSlim _tenantResolutionLock = new SemaphoreSlim(1, 1);
        
        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            // Get the request ID from the RequestLoggingMiddleware if available
            var requestId = context.Items.ContainsKey("RequestId") 
                ? context.Items["RequestId"].ToString() 
                : Guid.NewGuid().ToString();
                
            // Extract tenant slug from URL path
            var tenantSlug = context.GetTenantSlug();
            
            if (!string.IsNullOrEmpty(tenantSlug))
            {
                _logger.LogDebug("[{RequestId}] Resolving tenant for slug: {TenantSlug}", 
                    requestId, tenantSlug);
                
                // Store the tenant slug in HttpContext for potential fallback in controllers
                context.Items["TenantSlug"] = tenantSlug;
                
                // Try to resolve tenant
                if (!await ResolveTenantAsync(context, tenantContext, tenantSlug, requestId))
                {
                    return; // Response has been written, stop processing
                }
            }
            else
            {
                _logger.LogDebug("[{RequestId}] Path does not require tenant resolution: {Path}", 
                    requestId, context.Request.Path);
            }
            
            await _next(context);
        }
        
        private async Task<bool> ResolveTenantAsync(HttpContext context, ITenantContext tenantContext, 
            string tenantSlug, string requestId)
        {
            try
            {
                // Use a semaphore to prevent race conditions in tenant resolution
                // This helps when multiple requests come in at the same time
                await _tenantResolutionLock.WaitAsync();
                
                try
                {
                    // Check if tenant is already resolved with the same slug to avoid duplicate DB queries
                    if (context.Items.TryGetValue("CurrentTenantSlug", out var currentSlug) && 
                        currentSlug?.ToString() == tenantSlug && tenantContext.HasTenant)
                    {
                        _logger.LogDebug("[{RequestId}] Tenant already resolved for slug: {TenantSlug}", 
                            requestId, tenantSlug);
                        return true;
                    }
                    
                    // Use the tenant context to resolve the tenant
                    var success = await tenantContext.ResolveTenantFromSlugAsync(tenantSlug);
                    
                    if (success)
                    {
                        // Store the slug in HttpContext for future reference
                        context.Items["CurrentTenantSlug"] = tenantSlug;
                        return true;
                    }
                    
                    await HandleTenantNotFoundAsync(context, tenantSlug, requestId);
                    return false;
                }
                finally
                {
                    _tenantResolutionLock.Release();
                }
            }
            catch (Exception ex)
            {
                await HandleTenantResolutionErrorAsync(context, ex, requestId);
                return false;
            }
        }
        
        private async Task HandleTenantNotFoundAsync(HttpContext context, string tenantSlug, string requestId)
        {
            _logger.LogWarning("[{RequestId}] Tenant not found for slug: {TenantSlug}", 
                requestId, tenantSlug);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Tenant '{tenantSlug}' not found or inactive.");
        }
        
        private async Task HandleTenantResolutionErrorAsync(HttpContext context, Exception ex, string requestId)
        {
            _logger.LogError(ex, "[{RequestId}] Error resolving tenant: {Message}", 
                requestId, ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Error resolving tenant.");
        }
    }
    
    /// <summary>
    /// Extension methods for the TenantResolutionMiddleware
    /// </summary>
    public static class TenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolution(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantResolutionMiddleware>();
        }
    }
} 