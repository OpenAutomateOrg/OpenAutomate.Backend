using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System.Threading;

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

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, ITenantContext tenantContext)
        {
            // Get the request ID from the RequestLoggingMiddleware if available
            var requestId = context.Items.ContainsKey("RequestId") 
                ? context.Items["RequestId"].ToString() 
                : Guid.NewGuid().ToString();
                
            // URL format: https://domain.com/{tenantSlug}/api/...
            var path = context.Request.Path.Value;
            
            if (ShouldProcessPath(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var potentialTenantSlug = segments[0];
                
                // Skip tenant resolution for system endpoints
                if (IsSystemEndpoint(potentialTenantSlug))
                {
                    _logger.LogDebug("[{RequestId}] Skipping tenant resolution for system endpoint: {Path}", 
                        requestId, path);
                    await _next(context);
                    return;
                }
                
                _logger.LogDebug("[{RequestId}] Resolving tenant for slug: {TenantSlug}", 
                    requestId, potentialTenantSlug);
                
                // Store the tenant slug in HttpContext for potential fallback in controllers
                context.Items["TenantSlug"] = potentialTenantSlug;
                
                // Try to resolve tenant
                if (!await ResolveTenantAsync(context, unitOfWork, tenantContext, potentialTenantSlug, requestId))
                {
                    return; // Response has been written, stop processing
                }
            }
            else
            {
                _logger.LogDebug("[{RequestId}] Path does not require tenant resolution: {Path}", 
                    requestId, path);
            }
            
            await _next(context);
        }

        private bool ShouldProcessPath(string path)
        {
            return path != null && path.Length > 1 && path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length > 0;
        }
        
        private bool IsSystemEndpoint(string segment)
        {
            return segment == "api" || segment == "admin";
        }
        
        private async Task<bool> ResolveTenantAsync(HttpContext context, IUnitOfWork unitOfWork, 
            ITenantContext tenantContext, string tenantSlug, string requestId)
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
                    
                    // Clear any existing tenant to avoid stale data
                    tenantContext.ClearTenant();
                    
                    var tenant = await unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == tenantSlug && o.IsActive);
                    
                    if (tenant != null)
                    {
                        // Store the tenant in HttpContext.Items for later use
                        context.Items["CurrentTenant"] = tenant;
                        context.Items["CurrentTenantSlug"] = tenantSlug;
                        
                        // Set the tenant ID in the TenantContext service
                        tenantContext.SetTenant(tenant.Id);
                        
                        _logger.LogDebug("[{RequestId}] Tenant resolved: {TenantId}, {TenantName}, {TenantSlug}", 
                            requestId, tenant.Id, tenant.Name, tenantSlug);
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