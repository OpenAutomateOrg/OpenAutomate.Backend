using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for handling user account profile operations
    /// </summary>
    /// <remarks>
    /// Provides endpoints for retrieving comprehensive user profile information including
    /// permissions across all organization units the user belongs to.
    /// </remarks>
    [Route("api/account")]
    [ApiController]
    [Authorize]
    public class AccountController : CustomControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;

        // Define static log message templates for consistent logging
        private static class LogMessages
        {
            public const string ProfileRequested = "Profile requested for user: {UserId}";
            public const string ProfileRetrieved = "Profile retrieved successfully for user: {UserId}";
            public const string ProfileError = "Error retrieving profile for user: {UserId}";
            public const string UserNotAuthenticated = "Profile request from unauthenticated user";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class
        /// </summary>
        /// <param name="userService">The user service for profile operations</param>
        /// <param name="logger">The logger for recording profile operations</param>
        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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

                var profile = await _userService.GetUserProfileAsync(userId);

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