using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenAutomate.Core.Domain.Entities;
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

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, ITenantContext tenantContext)
        {
            // URL format: https://domain.com/{tenantSlug}/api/...
            var path = context.Request.Path.Value;
            
            // Clear any previous tenant context
            tenantContext.ClearTenant();
            
            if (path != null && path.Length > 1)
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    var potentialTenantSlug = segments[0];
                    
                    // Skip tenant resolution for system endpoints
                    if (potentialTenantSlug == "api" || potentialTenantSlug == "admin")
                    {
                        await _next(context);
                        return;
                    }
                    
                    var tenant = await unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == potentialTenantSlug && o.IsActive);
                    
                    if (tenant != null)
                    {
                        // Store the tenant in HttpContext.Items for direct access
                        context.Items["CurrentTenant"] = tenant;
                        
                        // Set the tenant in the tenant context service for query filters
                        tenantContext.SetTenant(tenant.Id);
                        
                        // Rewrite path to remove tenant segment
                        var newPath = "/" + string.Join('/', segments.Skip(1));
                        context.Request.Path = new PathString(newPath);
                    }
                    else
                    {
                        // Handle invalid tenant if needed
                        // context.Response.StatusCode = StatusCodes.Status404NotFound;
                        // await context.Response.WriteAsync("Tenant not found");
                        // return;
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