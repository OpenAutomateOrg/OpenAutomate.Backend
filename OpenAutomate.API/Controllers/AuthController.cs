using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegistrationRequest request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var response = await _userService.RegisterAsync(request, ipAddress);
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthenticationRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Email}", request.Email);
                var ipAddress = GetIpAddress();
                var response = await _userService.AuthenticateAsync(request, ipAddress);
                
                // Set refresh token in cookie
                SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);
                
                _logger.LogInformation("Login successful for user: {Email}", request.Email);
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("Login failed for user {Email}: {Message}", request.Email, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new { message = "Refresh token is required" });
                }

                var ipAddress = GetIpAddress();
                var response = await _userService.RefreshTokenAsync(refreshToken, ipAddress);
                
                // Set new refresh token in cookie
                SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);
                
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                // Accept token from request body or cookie
                var token = request.Token ?? Request.Cookies["refreshToken"];
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Token is required" });
                }

                var ipAddress = GetIpAddress();
                var success = await _userService.RevokeTokenAsync(token, ipAddress, request.Reason);
                
                if (!success)
                {
                    return NotFound(new { message = "Token not found" });
                }

                return Ok(new { message = "Token revoked" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        #region Helper Methods

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            // Get the current environment
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,          // Prevents client-side JS from accessing the cookie
                Expires = expires,
                SameSite = isDevelopment ? SameSiteMode.None : SameSiteMode.Lax, // None for local dev, Lax for production
                Secure = !isDevelopment,  // Only require HTTPS in non-development
                Path = "/api/auth/",      // Limit cookie to auth endpoints
                MaxAge = TimeSpan.FromDays(7) // Explicit max age as backup to Expires
            };

            _logger.LogDebug("Setting refresh token cookie. SameSite: {SameSite}, Secure: {Secure}, Expires: {Expires}", 
                cookieOptions.SameSite, cookieOptions.Secure, cookieOptions.Expires);
                
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"];
            }
            else
            {
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
            }
        }

        #endregion
    }
} 