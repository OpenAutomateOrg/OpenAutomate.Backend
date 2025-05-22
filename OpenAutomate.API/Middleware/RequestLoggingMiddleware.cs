using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace OpenAutomate.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Start timing the request
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId; // Store for potential use by other middleware

            // Capture the request details
            var request = context.Request;
            var requestMethod = request.Method;
            var requestPath = request.GetDisplayUrl();
            var userAgent = request.Headers.ContainsKey("User-Agent") ? request.Headers["User-Agent"].ToString() : "Unknown";
            var userId = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous";
            
            // Extract tenant information if available in the path
            var tenant = "unknown";
            var pathSegments = request.Path.Value?.Split('/') ?? Array.Empty<string>();
            if (pathSegments.Length > 1 && !string.IsNullOrWhiteSpace(pathSegments[1]))
            {
                tenant = pathSegments[1];
            }

            // Capture query parameters (with basic sanitization)
            var queryParams = string.Empty;
            if (request.QueryString.HasValue)
            {
                queryParams = SanitizeQueryString(request.QueryString.Value);
            }
            
            // Capture request content type and body size
            var contentType = request.ContentType ?? "none";
            var contentLength = request.ContentLength.HasValue ? request.ContentLength.Value : 0;

            // Log the incoming request with all details before any processing
            _logger.LogInformation(
                "BEGIN REQUEST {RequestId} - {RequestMethod} {RequestPath} - Tenant: {Tenant}, User: {UserId}, " +
                "Query: {QueryParams}, ContentType: {ContentType}, Size: {ContentLength}, UserAgent: {UserAgent}",
                requestId, requestMethod, requestPath, tenant, userId, 
                queryParams, contentType, contentLength, userAgent);

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);

                stopwatch.Stop();

                // Try to extract controller and action names after routing has occurred
                RouteData routeData = context.GetRouteData();
                string controller = string.Empty;
                string action = string.Empty;

                if (routeData != null)
                {
                    if (routeData.Values.TryGetValue("controller", out var controllerValue))
                    {
                        controller = controllerValue?.ToString() ?? string.Empty;
                    }
                    if (routeData.Values.TryGetValue("action", out var actionValue))
                    {
                        action = actionValue?.ToString() ?? string.Empty;
                    }
                }

                // Log the completed request with status code, duration, and controller/action info
                _logger.LogInformation(
                    "END REQUEST {RequestId} - {RequestMethod} {RequestPath} - Status: {StatusCode}, " +
                    "Duration: {ElapsedMilliseconds}ms, Controller: {Controller}, Action: {Action}",
                    requestId, requestMethod, requestPath, context.Response.StatusCode, 
                    stopwatch.ElapsedMilliseconds, controller, action);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log the exception with complete request details
                _logger.LogError(
                    ex,
                    "ERROR REQUEST {RequestId} - {RequestMethod} {RequestPath} - Tenant: {Tenant}, " +
                    "Status: {StatusCode}, Duration: {ElapsedMilliseconds}ms, Exception: {ExceptionType}",
                    requestId, requestMethod, requestPath, tenant,
                    context.Response?.StatusCode ?? 500, stopwatch.ElapsedMilliseconds, ex.GetType().Name);

                // Re-throw the exception to be handled by the global exception handler
                throw;
            }
        }

        private string SanitizeQueryString(string query)
        {
            // Basic sanitization to avoid logging sensitive data
            // Replace password or token parameters with [REDACTED]
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            var sensitiveParams = new[] { "password", "token", "secret", "key", "auth" };
            var sanitized = query;

            foreach (var param in sensitiveParams)
            {
                // Match parameters like password=something&
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized,
                    $"{param}=([^&]+)(&|$)",
                    $"{param}=[REDACTED]$2",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return sanitized;
        }
    }

    // Extension method for easy registration in Program.cs
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
} 