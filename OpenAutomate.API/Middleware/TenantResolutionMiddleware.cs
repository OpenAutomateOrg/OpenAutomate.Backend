using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Middleware
{
    /// <summary>
    /// Middleware that resolves the current tenant from the URL path
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;
        
        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, ITenantContext tenantContext)
        {
            // Clear any existing tenant context to avoid stale data between requests
            tenantContext.ClearTenant();
            
            // URL format: https://domain.com/{tenantSlug}/api/...
            var path = context.Request.Path.Value;
            
            if (path != null && path.Length > 1)
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    var potentialTenantSlug = segments[0];
                    
                    // Skip tenant resolution for system endpoints
                    if (potentialTenantSlug == "api" || potentialTenantSlug == "admin")
                    {
                        _logger.LogDebug("Skipping tenant resolution for system endpoint: {Path}", path);
                        await _next(context);
                        return;
                    }
                    
                    _logger.LogDebug("Resolving tenant for slug: {TenantSlug}", potentialTenantSlug);
                    
                    try
                    {
                        var tenant = await unitOfWork.OrganizationUnits
                            .GetFirstOrDefaultAsync(o => o.Slug == potentialTenantSlug && o.IsActive);
                        
                        if (tenant != null)
                        {
                            // Store the tenant in HttpContext.Items for later use
                            context.Items["CurrentTenant"] = tenant;
                            
                            // Set the tenant ID in the TenantContext service
                            tenantContext.SetTenant(tenant.Id);
                            
                            _logger.LogDebug("Tenant resolved: {TenantId}, {TenantName}", tenant.Id, tenant.Name);
                            
                            // Keep the original path with tenant segment
                            // No URL rewriting needed
                        }
                        else
                        {
                            _logger.LogWarning("Tenant not found for slug: {TenantSlug}", potentialTenantSlug);
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            await context.Response.WriteAsync($"Tenant '{potentialTenantSlug}' not found or inactive.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resolving tenant: {Message}", ex.Message);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Error resolving tenant.");
                        return;
                    }
                }
            }
            
            await _next(context);
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