using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Middleware
{
    /// <summary>
    /// Middleware that validates JWT token and retrieves the corresponding user
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, ITokenService tokenService,
            IConfiguration configuration)
        {
            var token = GetTokenFromRequest(context.Request);

            if (!string.IsNullOrEmpty(token))
            {
                if (tokenService.ValidateToken(token))
                {
                    // Extract user ID from token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);

                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                    {
                        // Get user from database
                        var user = await unitOfWork.Users.GetByIdAsync(userId);

                        if (user != null)
                        {
                            // Store the user in HttpContext.Items for later use
                            context.Items["User"] = user;
                        }
                    }
                }
            }

            await _next(context);
        }

        private string GetTokenFromRequest(HttpRequest request)
        {
            // Try to get the token from the Authorization header
            if (request.Headers.TryGetValue("Authorization", out StringValues authHeader))
            {
                var bearerToken = authHeader.FirstOrDefault();

                if (!string.IsNullOrEmpty(bearerToken) &&
                    bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return bearerToken.Substring(7); // Remove "Bearer " prefix
                }
            }

            // Or from a query parameter (optional)
            if (request.Query.TryGetValue("token", out StringValues queryToken))
            {
                return queryToken.FirstOrDefault();
            }

            return null;
        }
    }
} 