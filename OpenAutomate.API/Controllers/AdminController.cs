using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.AdminDto;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : CustomControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves detailed information for a specific user by their ID. Only accessible by administrators.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve.</param>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Updates the first and last name of a user. Only accessible by administrators.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="request">The new first and last name information.</param>
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserInfo(Guid userId, [FromBody] UpdateUserInfoRequest request)
        {
            try
            {
                var response = await _adminService.UpdateUserInfoAsync(userId, request);
                return Ok(response);
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Admin failed to update user info for user: {UserId}", userId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user info by admin for user: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Changes the password of a user. Only accessible by administrators.
        /// </summary>
        /// <param name="userId">The ID of the user whose password will be changed.</param>
        /// <param name="request">The new password information.</param>
        [HttpPost("change-password/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword(Guid userId, [FromBody] AdminChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest(new { message = "New password and confirm password do not match." });

            try
            {
                var result = await _adminService.ChangePasswordAsync(userId, request.NewPassword);
                if (!result)
                    return BadRequest(new { message = "Password change failed" });
                return Ok(new { message = "Password changed successfully" });
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Admin failed to change password for user: {UserId}", userId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password by admin for user: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
    }
}
