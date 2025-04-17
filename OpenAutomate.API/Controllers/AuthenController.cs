using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [Route("api/authen")]
    [ApiController]
    [Authentication]
    public class AuthenController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthenController> _logger;

        public AuthenController(IUserService userService, ILogger<AuthenController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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

        [AllowAnonymous]
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
                var success = await _userService.RevokeTokenAsync(token, ipAddress, "Revoke Token");
                
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

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                if (user == null)
                {
                    _logger.LogWarning("User not found in HttpContext");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Convert the user entity to a DTO directly without making another DB call
                var userResponse = _userService.MapToResponse(user);

                _logger.LogInformation("Retrieved user information for user ID: {UserId}", user.Id);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user information");
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
                SameSite = SameSiteMode.None, // Use None for both development and production with CORS
                Secure = true,            // Always use secure cookies (required with SameSite=None)
                Path = "/",               // Make cookie available to all paths
                MaxAge = TimeSpan.FromDays(7) // Explicit max age as backup to Expires
            };

            _logger.LogDebug("Setting refresh token cookie. SameSite: {SameSite}, Secure: {Secure}, Expires: {Expires}, Path: {Path}", 
                cookieOptions.SameSite, cookieOptions.Secure, cookieOptions.Expires, cookieOptions.Path);
                
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