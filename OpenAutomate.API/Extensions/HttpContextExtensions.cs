using Microsoft.AspNetCore.Http;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.API.Extensions
{
    /// <summary>
    /// Extension methods for HttpContext
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the current user from the HttpContext items
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <returns>The current User or null if not authenticated</returns>
        public static User GetCurrentUser(this HttpContext context)
        {
            if (context.Items.TryGetValue("User", out var user))
            {
                return user as User;
            }
            
            return null;
        }
        public static bool IsAuthenticated(this HttpContext context)
        {
            return context.GetCurrentUser() != null;
        }

        /// <summary>
        /// Extracts the tenant slug from the request path
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>The tenant slug or null if not found</returns>
        public static string GetTenantSlug(this HttpContext context)
        {
            if (context == null)
                return null;
                
            return GetTenantSlugFromPath(context.Request?.Path);
        }
        
        /// <summary>
        /// Helper method to extract tenant slug from the request path
        /// </summary>
        /// <param name="path">The request path</param>
        /// <returns>The tenant slug or null if not found</returns>
        public static string GetTenantSlugFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
                
            // URL format: /{tenant}/api/...
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var potentialSlug = segments[0];
                
                // Skip system endpoints
                if (potentialSlug == "api" || potentialSlug == "admin")
                    return null;
                    
                return potentialSlug;
            }
            
            return null;
        }
    }
} 