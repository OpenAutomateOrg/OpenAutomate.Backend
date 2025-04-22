using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for handling user authentication and account management
    /// </summary>
    /// <remarks>
    /// Provides endpoints for user registration, login, token refresh, and token revocation.
    /// </remarks>
    [Route("api/authen")]
    [ApiController]
    public class AuthenController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthenController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenController"/> class
        /// </summary>
        /// <param name="userService">The user service for authentication operations</param>
        /// <param name="logger">The logger for recording authentication events</param>
        public AuthenController(IUserService userService, ILogger<AuthenController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <param name="request">The registration information containing email, password, and other user details</param>
        /// <returns>User registration confirmation with authentication tokens</returns>
        /// <response code="200">Registration successful</response>
        /// <response code="400">Invalid registration data or email already exists</response>
        /// <response code="500">Server error during registration process</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Authenticates a user and provides access tokens
        /// </summary>
        /// <param name="request">The authentication request containing email and password</param>
        /// <returns>Authentication response with access token and refresh token</returns>
        /// <response code="200">Authentication successful</response>
        /// <response code="400">Invalid credentials or account disabled</response>
        /// <response code="500">Server error during authentication process</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Generates a new access token using a valid refresh token
        /// </summary>
        /// <returns>Authentication response with new access token and refresh token</returns>
        /// <remarks>
        /// The refresh token is extracted from the HTTP-only cookie set during login
        /// </remarks>
        /// <response code="200">Token refresh successful</response>
        /// <response code="400">Refresh token missing or invalid</response>
        /// <response code="500">Server error during token refresh process</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Revokes an active refresh token to prevent future use
        /// </summary>
        /// <param name="request">Optional revocation request containing token and reason</param>
        /// <returns>Confirmation of token revocation</returns>
        /// <remarks>
        /// The refresh token can either be provided in request body or extracted from the cookie.
        /// This endpoint requires authentication.
        /// </remarks>
        /// <response code="200">Token successfully revoked</response>
        /// <response code="400">Token is missing</response>
        /// <response code="404">Token not found or already revoked</response>
        /// <response code="500">Server error during revocation process</response>
        [Authorize]
        [HttpPost("revoke-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Gets the profile information for the currently authenticated user
        /// </summary>
        /// <returns>User profile details of the current user</returns>
        /// <remarks>
        /// This endpoint requires authentication.
        /// </remarks>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error while retrieving user information</response>
        [Authorize]
        [HttpGet("user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Sets the refresh token in an HTTP-only cookie
        /// </summary>
        /// <param name="token">The refresh token value</param>
        /// <param name="expires">The expiration date for the token</param>
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

        /// <summary>
        /// Gets the client's IP address from request headers or connection information
        /// </summary>
        /// <returns>The client's IP address or "unknown" if not available</returns>
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