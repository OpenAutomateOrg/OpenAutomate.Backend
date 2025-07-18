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
            if (string.IsNullOrEmpty(token) || !tokenService.ValidateToken(token))
            {
                await _next(context);
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var jtiClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);

            if (await IsTokenBlocklisted(jtiClaim, jwtBlocklistService))
            {
                await _next(context);
                return;
            }

            if (await IsUserBlocked(userIdClaim, jwtBlocklistService))
            {
                await _next(context);
                return;
            }

            await SetUserPrincipal(context, unitOfWork, userIdClaim);
            await _next(context);
        }

        private async Task<bool> IsTokenBlocklisted(Claim? jtiClaim, IJwtBlocklistService jwtBlocklistService)
        {
            if (jtiClaim == null) return false;
            return await jwtBlocklistService.IsTokenBlocklistedAsync(jtiClaim.Value);
        }

        private async Task<bool> IsUserBlocked(Claim? userIdClaim, IJwtBlocklistService jwtBlocklistService)
        {
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId)) return false;
            return await jwtBlocklistService.IsUserBlocklistedAsync(userId);
        }

        private async Task SetUserPrincipal(HttpContext context, IUnitOfWork unitOfWork, Claim? userIdClaim)
        {
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId)) return;
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return;

            context.Items["User"] = user;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.SystemRole.ToString())
            };
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;
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