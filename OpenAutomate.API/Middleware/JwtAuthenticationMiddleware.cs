using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System.Security.Claims;

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
            IJwtBlocklistService jwtBlocklistService, IConfiguration configuration)
        {
            var token = GetTokenFromRequest(context.Request);

            if (!string.IsNullOrEmpty(token))
            {
                if (tokenService.ValidateToken(token))
                {
                    // Extract JWT ID and user ID from token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
                    var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);

                    // Check if token is blocklisted
                    if (jtiClaim != null)
                    {
                        var isBlocklisted = await jwtBlocklistService.IsTokenBlocklistedAsync(jtiClaim.Value);
                        if (isBlocklisted)
                        {
                            // Token is blocklisted, do not authenticate
                            await _next(context);
                            return;
                        }
                    }

                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                    {
                        // Check if all user tokens are blocked
                        var isUserBlocked = await jwtBlocklistService.IsUserBlocklistedAsync(userId);
                        if (isUserBlocked)
                        {
                            // All user tokens are blocked, do not authenticate
                            await _next(context);
                            return;
                        }

                        // Get user from database
                        var user = await unitOfWork.Users.GetByIdAsync(userId);

                        if (user != null)
                        {
                            // Store the user in HttpContext.Items for backward compatibility
                            context.Items["User"] = user;

                            // Create ClaimsPrincipal for built-in authorization framework
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim(ClaimTypes.Role, user.SystemRole.ToString())
                            };

                            var identity = new ClaimsIdentity(claims, "jwt");
                            var principal = new ClaimsPrincipal(identity);

                            // Set the user principal for built-in [Authorize] attribute
                            context.User = principal;
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