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
                _logger.LogInformation("Processing email verification for token");

                var userId = await _tokenService.ValidateEmailVerificationTokenAsync(token);
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Invalid or expired verification token");
                    return Redirect($"{_appSettings.FrontendUrl}/email-verified?success=false&reason=invalid-token");
                }

                var result = await _userService.VerifyUserEmailAsync(userId.Value);
                if (!result)
                {
                    _logger.LogWarning("Failed to verify email for user ID: {UserId}", userId);
                    return Redirect($"{_appSettings.FrontendUrl}/email-verified?success=false&reason=verification-failed");
                }

                // Get user info
                var user = await _userService.GetByIdAsync(userId.Value);

                // Send welcome email
                await _notificationService.SendWelcomeEmailAsync(user.Email, $"{user.FirstName} {user.LastName}");

                _logger.LogInformation("Email verified successfully for user ID: {UserId}", userId);
                // Redirect to frontend with success message
                return Redirect($"{_appSettings.FrontendUrl}/email-verified?success=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email verification");
                return Redirect($"{_appSettings.FrontendUrl}/email-verified?success=false&reason=server-error");
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
    }
} 