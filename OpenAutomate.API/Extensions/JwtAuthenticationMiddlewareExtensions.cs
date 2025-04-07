using OpenAutomate.API.Middleware;

namespace OpenAutomate.API.Extensions;

/// <summary>
/// Extension methods for the JwtAuthenticationMiddleware
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}