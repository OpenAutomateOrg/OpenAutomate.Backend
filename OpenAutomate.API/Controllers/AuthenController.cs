using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System.Diagnostics.CodeAnalysis;

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
        private readonly ITenantContext _tenantContext;

        // Define static log message templates for consistent logging
        private static class LogMessages
        {
            public const string UserRegistered = "User registered successfully: {Email}. Verification email sent.";
            public const string RegistrationFailed = "Registration failed: {Message}";
            public const string RegistrationError = "Error during registration: {Message}";
            
            public const string LoginAttempt = "Login attempt for user: {Email}";
            public const string LoginSuccess = "Login successful for user: {Email}";
            public const string LoginFailed = "Login failed for user {Email}: {Message}";
            public const string LoginError = "Error during login for user {Email}: {Message}";
            
            public const string TokenRefreshMissing = "Refresh token is missing in request cookies";
            public const string TokenRefreshProcessing = "Processing refresh token request with token: {Token}, Client IP: {IpAddress}";
            public const string TokenRefreshSuccess = "Token refreshed successfully for user: {UserId}, {Email}";
            public const string TokenRefreshFailed = "Token refresh failed: {Message}";
            public const string TokenRefreshError = "Error during token refresh: {Message}";
            
            public const string TokenRevocationError = "Error during token revocation: {Message}";
            
            public const string UserNotFound = "User not found in HttpContext";
            public const string UserInfoRetrieved = "Retrieved user information for user ID: {UserId}";
            public const string UserInfoError = "Error retrieving user information: {Message}";
            
            public const string CookieSet = "Setting refresh token cookie. Token: {TokenPreview}, SameSite: {SameSite}, Secure: {Secure}, Expires: {Expires}";
            public const string CookieSetSuccess = "Refresh token cookie set successfully. Expires: {Expires}";
            public const string CookieSetError = "Error setting refresh token cookie: {Message}";
            
            public const string TenantContextSet = "Set default system tenant context for authentication operation";
            public const string TenantContextError = "Error setting tenant context: {Message}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenController"/> class
        /// </summary>
        /// <param name="userService">The user service for authentication operations</param>
        /// <param name="logger">The logger for recording authentication events</param>
        /// <param name="tenantContext">The tenant context for current tenant information</param>
        public AuthenController(
            IUserService userService, 
            ILogger<AuthenController> logger,
            ITenantContext tenantContext)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
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
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { message = "Registration request cannot be null" });
                }

                var ipAddress = GetIpAddress();
                var response = await _userService.RegisterAsync(request, ipAddress);

                // Send email verification
                await _userService.SendVerificationEmailAsync(response.Id);
                
                _logger.LogInformation(LogMessages.UserRegistered, request.Email);
                return Ok(new { 
                    user = response,
                    message = "Registration successful. Please check your email to verify your account." 
                });
            }
            catch (UserAlreadyExistsException ex)
            {
                _logger.LogWarning(LogMessages.RegistrationFailed, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(LogMessages.RegistrationFailed, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(LogMessages.RegistrationFailed, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.RegistrationError, ex.Message);
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
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { message = "Authentication request cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }

                _logger.LogInformation(LogMessages.LoginAttempt, request.Email);
                var ipAddress = GetIpAddress();
                
                try
                {
                    // Set default tenant if not already set
                    EnsureDefaultTenant();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogMessages.TenantContextError, ex.Message);
                    throw new InvalidOperationException("Unable to establish tenant context for authentication", ex);
                }
                
                var response = await _userService.AuthenticateAsync(request, ipAddress);
                
                try
                {
                    // Set refresh token in cookie
                    SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request if cookie setting fails
                    _logger.LogError(ex, LogMessages.CookieSetError, ex.Message);
                }
                
                _logger.LogInformation(LogMessages.LoginSuccess, request.Email);
                return Ok(response);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning(LogMessages.LoginFailed, request?.Email ?? "unknown", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (EmailVerificationRequiredException ex)
            {
                _logger.LogWarning(LogMessages.LoginFailed, request?.Email ?? "unknown", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(LogMessages.LoginFailed, request?.Email ?? "unknown", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LoginError, request?.Email ?? "unknown", ex.Message);
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
                    _logger.LogWarning(LogMessages.TokenRefreshMissing);
                    return BadRequest(new { message = "Refresh token is required" });
                }

                EnsureDefaultTenant();

                // Get client IP for tracking
                var ipAddress = GetIpAddress();
                string tokenPreview = refreshToken.Length > 10 ? 
                    refreshToken.Substring(0, Math.Min(10, refreshToken.Length)) : 
                    "[invalid token]";
                
                _logger.LogInformation(LogMessages.TokenRefreshProcessing, tokenPreview, ipAddress);
                
                // Attempt to refresh the token
                var response = await _userService.RefreshTokenAsync(refreshToken, ipAddress);
                
                _logger.LogInformation(LogMessages.TokenRefreshSuccess, response.Id, response.Email);
                
                try
                {
                    // Set new refresh token in cookie
                    SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiration);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request if cookie setting fails
                    _logger.LogError(ex, LogMessages.CookieSetError, ex.Message);
                }
                
                return Ok(response);
            }
            catch (TokenException ex)
            {
                _logger.LogWarning(LogMessages.TokenRefreshFailed, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(LogMessages.TokenRefreshFailed, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.TokenRefreshError, ex.Message);
                return StatusCode(500, new { message = "An error occurred while refreshing your token." });
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
                var token = request?.Token ?? Request.Cookies["refreshToken"];
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Token is required" });
                }

                EnsureDefaultTenant();

                var ipAddress = GetIpAddress();
                var success = await _userService.RevokeTokenAsync(
                    token, 
                    ipAddress, 
                    request?.Reason ?? string.Empty);
                
                if (!success)
                {
                    return NotFound(new { message = "Token not found" });
                }

                return Ok(new { message = "Token revoked" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.TokenRevocationError, ex.Message);
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
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var user = HttpContext.Items["User"] as User;
                if (user == null)
                {
                    _logger.LogWarning(LogMessages.UserNotFound);
                    return Unauthorized(new { message = "User not authenticated" });
                }

                EnsureDefaultTenant();

                // Convert the user entity to a DTO directly without making another DB call
                var userResponse = _userService.MapToResponse(user);

                _logger.LogInformation(LogMessages.UserInfoRetrieved, user.Id);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.UserInfoError, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving user information." });
            }
        }

        /// <summary>
        /// Sends a password reset email to the user
        /// </summary>
        /// <param name="request">The forgot password request containing user's email</param>
        /// <returns>Success message if reset email was sent</returns>
        /// <response code="200">Reset email sent successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Server error</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }

                // Set default tenant if not already set
                EnsureDefaultTenant();
                
                var result = await _userService.ForgotPasswordAsync(request.Email);
                
                // Always return success to prevent email enumeration attacks
                // This way, attackers can't determine if an email exists in the system
                return Ok(new { message = "If your email is registered in our system, you will receive password reset instructions." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Resets the user's password using a valid token
        /// </summary>
        /// <param name="request">The reset password request containing email, token, and new password</param>
        /// <returns>Success message if password was reset successfully</returns>
        /// <response code="200">Password reset successful</response>
        /// <response code="400">Invalid request or token</response>
        /// <response code="500">Server error</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = "Email, token, and new password are required" });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "New password and confirmation do not match" });
                }

                // Set default tenant if not already set
                EnsureDefaultTenant();
                
                var result = await _userService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
                
                if (!result)
                {
                    return BadRequest(new { message = "Password reset failed. The token may be invalid or expired." });
                }
                
                return Ok(new { message = "Your password has been reset successfully. You can now log in with your new password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset: {Message}", ex.Message);
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
            ArgumentNullException.ThrowIfNull(token, nameof(token));
            
            try
            {
                // Configure cookie options
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,          // Prevents client-side JS from accessing the cookie
                    Expires = expires,
                    SameSite = SameSiteMode.None, // Use None for both development and production with CORS
                    Secure = true,            // Always use secure cookies (required with SameSite=None)
                    Path = "/",               // Make cookie available to all paths
                    MaxAge = TimeSpan.FromDays(7) // Explicit max age as backup to Expires
                };

                string tokenPreview = token.Length > 10 ? 
                    token.Substring(0, Math.Min(10, token.Length)) : 
                    "[invalid token]";
                
                _logger.LogDebug(LogMessages.CookieSet, 
                    tokenPreview, cookieOptions.SameSite, cookieOptions.Secure, cookieOptions.Expires);
                    
                // Clear any existing cookie first to ensure we're not having duplicates
                Response.Cookies.Delete("refreshToken");
                
                // Add the new cookie
                Response.Cookies.Append("refreshToken", token, cookieOptions);
                
                _logger.LogInformation(LogMessages.CookieSetSuccess, expires);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.CookieSetError, ex.Message);
                throw new InvalidOperationException("Failed to set refresh token cookie", ex);
            }
        }

        /// <summary>
        /// Gets the client's IP address from request headers or connection information
        /// </summary>
        /// <returns>The client's IP address or "unknown" if not available</returns>
        private string GetIpAddress()
        {
            try
            {
                // Get the forwarded header through model binding
                var forwardedForHeader = GetForwardedForHeader();
                
                if (!string.IsNullOrEmpty(forwardedForHeader))
                {
                    // X-Forwarded-For can contain multiple IPs, use the first one (client IP)
                    return forwardedForHeader.Split(',')[0].Trim();
                }
                
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
            }
            catch (Exception)
            {
                // Don't throw exceptions for IP address retrieval failures
                return "unknown";
            }
        }

        /// <summary>
        /// Uses model binding to retrieve the X-Forwarded-For header
        /// </summary>
        [NonAction]
        public string GetForwardedForHeader([FromHeader(Name = "X-Forwarded-For")] string? forwardedFor = null)
        {
            return forwardedFor ?? string.Empty;
        }
        
        /// <summary>
        /// Ensures a default tenant is set in the tenant context for authentication operations
        /// </summary>
        /// <remarks>
        /// Authentication operations do not rely on tenant-specific data
        /// but services may require a valid tenant context
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when tenant context could not be set</exception>
        private void EnsureDefaultTenant()
        {
            try
            {
                if (!_tenantContext.HasTenant)
                {
                    // Use the system tenant for authentication operations
                    // This is a special tenant ID used for system-wide operations
                    Guid systemTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    _tenantContext.SetTenant(systemTenantId);
                    _logger.LogDebug(LogMessages.TenantContextSet);
                }
            }
            catch (FormatException ex)
            {
                string errorMessage = "Invalid system tenant ID format";
                _logger.LogError(ex, LogMessages.TenantContextError, errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = "Failed to set tenant context";
                _logger.LogError(ex, LogMessages.TenantContextError, errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        #endregion
    }
} 