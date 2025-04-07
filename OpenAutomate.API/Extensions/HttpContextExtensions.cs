using Microsoft.AspNetCore.Http;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.API.Extensions
{
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
    }
} 