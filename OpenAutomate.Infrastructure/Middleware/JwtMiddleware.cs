using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenAutomate.Domain.Interfaces.IJwtUtils;
using OpenAutomate.Infrastructure.DbContext;
using System.Security.Claims;

namespace OpenAutomate.Infrastructure.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext dbContext, IJwtUtils jwtUtils)
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                
                if (string.IsNullOrEmpty(token))
                {
                    await _next(context);
                    return;
                }

                if (!jwtUtils.ValidateJwtToken(token, out string userId))
                {
                    _logger.LogWarning("Invalid JWT token received");
                    await _next(context);
                    return;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("JWT token validation succeeded but no user ID was extracted");
                    await _next(context);
                    return;
                }

                var user = await dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId);
                    await _next(context);
                    return;
                }

                // Attach user to context
                context.Items["User"] = user;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JWT token");
                await _next(context);
            }
        }
    }
}
