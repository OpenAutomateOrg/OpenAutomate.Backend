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
            
            if (ShouldProcessPath(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var potentialTenantSlug = segments[0];
                
                // Skip tenant resolution for system endpoints
                if (IsSystemEndpoint(potentialTenantSlug))
                {
                    _logger.LogDebug("Skipping tenant resolution for system endpoint: {Path}", path);
                    await _next(context);
                    return;
                }
                
                _logger.LogDebug("Resolving tenant for slug: {TenantSlug}", potentialTenantSlug);
                
                if (!await ResolveTenantAsync(context, unitOfWork, tenantContext, potentialTenantSlug))
                {
                    return; // Response has been written, stop processing
                }
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
            ITenantContext tenantContext, string tenantSlug)
        {
            try
            {
                var tenant = await unitOfWork.OrganizationUnits
                    .GetFirstOrDefaultAsync(o => o.Slug == tenantSlug && o.IsActive);
                
                if (tenant != null)
                {
                    // Store the tenant in HttpContext.Items for later use
                    context.Items["CurrentTenant"] = tenant;
                    
                    // Set the tenant ID in the TenantContext service
                    tenantContext.SetTenant(tenant.Id);
                    
                    _logger.LogDebug("Tenant resolved: {TenantId}, {TenantName}", tenant.Id, tenant.Name);
                    return true;
                }
                
                await HandleTenantNotFoundAsync(context, tenantSlug);
                return false;
            }
            catch (Exception ex)
            {
                await HandleTenantResolutionErrorAsync(context, ex);
                return false;
            }
        }
        
        private async Task HandleTenantNotFoundAsync(HttpContext context, string tenantSlug)
        {
            _logger.LogWarning("Tenant not found for slug: {TenantSlug}", tenantSlug);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Tenant '{tenantSlug}' not found or inactive.");
        }
        
        private async Task HandleTenantResolutionErrorAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant: {Message}", ex.Message);
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