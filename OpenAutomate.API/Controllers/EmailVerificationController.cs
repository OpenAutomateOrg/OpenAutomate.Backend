using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for handling email verification
    /// </summary>
    [ApiController]
    [Route("api/email")]
    public class EmailVerificationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly INotificationService _notificationService;
        private readonly AppSettings _appSettings;
        private readonly ILogger<EmailVerificationController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailVerificationController"/> class
        /// </summary>
        public EmailVerificationController(
            IUserService userService,
            ITokenService tokenService,
            INotificationService notificationService,
            IOptions<AppSettings> appSettings,
            ILogger<EmailVerificationController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _notificationService = notificationService;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Verifies a user's email using a verification token
        /// </summary>
        /// <param name="token">The verification token</param>
        /// <returns>Redirect to the frontend with success or error status</returns>
        [HttpGet("verify")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                _logger.LogInformation("Processing email verification for token: {TokenPrefix}...", 
                    token?.Length > 6 ? token.Substring(0, 6) + "..." : "null");
                
                _logger.LogInformation("Frontend URL from configuration: {FrontendUrl}", _appSettings.FrontendUrl);
                _logger.LogInformation("Request path: {Path}, QueryString: {QueryString}", Request.Path, Request.QueryString);

                var userId = await _tokenService.ValidateEmailVerificationTokenAsync(token);
                _logger.LogInformation("Token validation result - UserId: {UserId}", userId);
                
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Invalid or expired verification token");
                    var redirectUrl = $"{_appSettings.FrontendUrl}/email-verified?success=false&reason=invalid-token";
                    _logger.LogInformation("Redirecting to: {RedirectUrl}", redirectUrl);
                    return Redirect(redirectUrl);
                }
                
                _logger.LogInformation("Token validated successfully for user ID: {UserId}", userId);

                // Kiểm tra trạng thái xác thực hiện tại
                var userBeforeUpdate = await _userService.GetByIdAsync(userId.Value);
                _logger.LogInformation("User before verification - Email: {Email}, IsEmailVerified: {IsVerified}", 
                    userBeforeUpdate.Email, userBeforeUpdate.IsEmailVerified);
                
                if (userBeforeUpdate.IsEmailVerified)
                {
                    _logger.LogInformation("User {UserId} is already verified, redirecting to success page", userId);
                    var alreadyVerifiedUrl = $"{_appSettings.FrontendUrl}/email-verified?success=true&reason=already-verified";
                    _logger.LogInformation("Redirecting to: {RedirectUrl}", alreadyVerifiedUrl);
                    return Redirect(alreadyVerifiedUrl);
                }

                var result = await _userService.VerifyUserEmailAsync(userId.Value);
                _logger.LogInformation("VerifyUserEmailAsync result: {Result}", result);
                
                if (!result)
                {
                    _logger.LogWarning("Failed to verify email for user ID: {UserId}", userId);
                    var failedUrl = $"{_appSettings.FrontendUrl}/email-verified?success=false&reason=verification-failed";
                    _logger.LogInformation("Redirecting to: {RedirectUrl}", failedUrl);
                    return Redirect(failedUrl);
                }
                
                _logger.LogInformation("Email verification status updated successfully for user ID: {UserId}", userId);

                // Get user info after update
                var user = await _userService.GetByIdAsync(userId.Value);
                _logger.LogInformation("Retrieved user info after verification - Email: {Email}, IsEmailVerified: {IsVerified}", 
                    user.Email, user.IsEmailVerified);

                // Send welcome email
                _logger.LogInformation("Sending welcome email to: {Email}", user.Email);
                await _notificationService.SendWelcomeEmailAsync(user.Email, $"{user.FirstName} {user.LastName}");
                _logger.LogInformation("Welcome email sent successfully");

                _logger.LogInformation("Email verification process completed for user ID: {UserId}, IsVerified: {IsVerified}", 
                    userId, user.IsEmailVerified);
                    
                // Redirect to frontend with success message
                var successUrl = $"{_appSettings.FrontendUrl}/email-verified?success=true";
                _logger.LogInformation("Redirecting to success page: {RedirectUrl}", successUrl);
                return Redirect(successUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email verification");
                var errorUrl = $"{_appSettings.FrontendUrl}/email-verified?success=false&reason=server-error";
                _logger.LogInformation("Redirecting to error page: {RedirectUrl}", errorUrl);
                return Redirect(errorUrl);
            }
        }

        /// <summary>
        /// Resends a verification email to the currently authenticated user
        /// </summary>
        /// <returns>Status of the operation</returns>
        [HttpPost("resend")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendVerification()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("User ID not found in claims for resend verification request");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userService.GetByIdAsync(userGuid);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userGuid);
                    return NotFound(new { message = "User not found" });
                }

                if (user.IsEmailVerified)
                {
                    _logger.LogInformation("Email is already verified for user: {UserId}", userGuid);
                    return BadRequest(new { message = "Email is already verified" });
                }

                var success = await _userService.SendVerificationEmailAsync(userGuid);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to send verification email" });
                }

                _logger.LogInformation("Verification email resent for user: {UserId}", userGuid);
                return Ok(new { message = "Verification email sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
        
        /// <summary>
        /// Resends a verification email to a user by email address (without requiring authentication)
        /// </summary>
        /// <param name="email">The email address to resend verification to</param>
        /// <returns>Status of the operation</returns>
        [HttpPost("resend-by-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendVerificationByEmail([FromBody] ResendVerificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }
                
                _logger.LogInformation("Processing resend verification request for email: {Email}", request.Email);
                
                // Find user by email
                var user = await _userService.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", request.Email);
                    return NotFound(new { message = "User not found" });
                }
                
                if (user.IsEmailVerified)
                {
                    _logger.LogInformation("Email is already verified for user: {Email}", request.Email);
                    return BadRequest(new { message = "Email is already verified" });
                }
                
                var success = await _userService.SendVerificationEmailAsync(user.Id);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to send verification email" });
                }
                
                _logger.LogInformation("Verification email resent for user: {Email}", request.Email);
                return Ok(new { message = "Verification email sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
        
        /// <summary>
        /// Checks if an email is verified
        /// </summary>
        /// <param name="email">The email address to check</param>
        /// <returns>Status of the email verification</returns>
        [HttpGet("check-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckEmailVerificationStatus([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email is required" });
                }
                
                _logger.LogInformation("Checking verification status for email: {Email}", email);
                
                // Find user by email
                var user = await _userService.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound(new { message = "User not found" });
                }
                
                return Ok(new { 
                    email = user.Email,
                    isVerified = user.IsEmailVerified,
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
    }
    
    public class ResendVerificationRequest
    {
        public string Email { get; set; }
    }
} 