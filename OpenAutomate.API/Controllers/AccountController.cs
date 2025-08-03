using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Exceptions;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for handling user account self-service operations
    /// </summary>
    /// <remarks>
    /// Provides endpoints for all self-service account operations including:
    /// - Retrieving user profile information with permissions
    /// - Updating personal information (first name, last name)
    /// - Changing password
    /// All operations are performed on the currently authenticated user's own account.
    /// </remarks>
    [Route("api/account")]
    [ApiController]
    [Authorize]
    public class AccountController : CustomControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        // Define static log message templates for consistent logging
        private static class LogMessages
        {
            public const string ProfileRequested = "Profile requested for user: {UserId}";
            public const string ProfileRetrieved = "Profile retrieved successfully for user: {UserId}";
            public const string ProfileError = "Error retrieving profile for user: {UserId}";
            public const string UserNotAuthenticated = "Profile request from unauthenticated user";
            
            public const string InfoUpdateRequested = "Info update requested for user: {UserId}";
            public const string InfoUpdateSuccess = "Info updated successfully for user: {UserId}";
            public const string InfoUpdateError = "Error updating info for user: {UserId}";
            
            public const string PasswordChangeRequested = "Password change requested for user: {UserId}";
            public const string PasswordChangeSuccess = "Password changed successfully for user: {UserId}";
            public const string PasswordChangeError = "Error changing password for user: {UserId}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class
        /// </summary>
        /// <param name="accountService">The account service for account operations</param>
        /// <param name="logger">The logger for recording account operations</param>
        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the complete user profile with permissions across all organization units
        /// </summary>
        /// <returns>Complete user profile including system role, organization units, and permissions</returns>
        /// <remarks>
        /// This endpoint returns comprehensive profile information including:
        /// - Basic user information (name, email, system role)
        /// - All organization units the user belongs to
        /// - All permissions for each organization unit (resource + permission level)
        /// 
        /// The permissions are aggregated to show the highest permission level for each resource
        /// across all roles the user has within each organization unit.
        /// </remarks>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Server error during profile retrieval</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                _logger.LogInformation(LogMessages.ProfileRequested, userId);

                var profile = await _accountService.GetUserProfileAsync(userId);

                _logger.LogInformation(LogMessages.ProfileRetrieved, userId);

                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(LogMessages.UserNotAuthenticated);
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                var userId = GetCurrentUserIdSafe();
                _logger.LogError(ex, LogMessages.ProfileError, userId);
                return StatusCode(500, new { message = "An error occurred while retrieving your profile." });
            }
        }

        /// <summary>
        /// Updates the current user's personal information (first name and last name)
        /// </summary>
        /// <param name="request">The update request containing new first name and last name</param>
        /// <returns>The updated user information</returns>
        /// <response code="200">User information updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error during update process</response>
        [HttpPut("info")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateUserInfoRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                _logger.LogInformation(LogMessages.InfoUpdateRequested, userId);
                
                var response = await _accountService.UpdateUserInfoAsync(userId, request);
                
                _logger.LogInformation(LogMessages.InfoUpdateSuccess, userId);
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(LogMessages.UserNotAuthenticated);
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                var userId = GetCurrentUserIdSafe();
                _logger.LogError(ex, LogMessages.InfoUpdateError, userId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Changes the current user's password
        /// </summary>
        /// <param name="request">The password change request containing current password and new password</param>
        /// <returns>Success message if password was changed</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid request data or current password is incorrect</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error during password change process</response>
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                _logger.LogInformation(LogMessages.PasswordChangeRequested, userId);
                
                var result = await _accountService.ChangePasswordAsync(userId, request);
                if (!result)
                    return BadRequest(new { message = "Password change failed" });
                    
                _logger.LogInformation(LogMessages.PasswordChangeSuccess, userId);
                
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(LogMessages.UserNotAuthenticated);
                return Unauthorized(new { message = "User not authenticated" });
            }
            catch (ServiceException ex)
            {
                var userId = GetCurrentUserIdSafe();
                _logger.LogWarning("Service exception during password change for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var userId = GetCurrentUserIdSafe();
                _logger.LogError(ex, LogMessages.PasswordChangeError, userId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Safely gets the current user ID without throwing exceptions
        /// </summary>
        /// <returns>The current user ID or "unknown" if not available</returns>
        private string GetCurrentUserIdSafe()
        {
            try
            {
                return GetCurrentUserId().ToString();
            }
            catch
            {
                return "unknown";
            }
        }
    }
} 