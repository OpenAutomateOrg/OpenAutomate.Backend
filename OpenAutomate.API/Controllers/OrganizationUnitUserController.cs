using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace OpenAutomate.API.Controllers
{
    [Route("api/ou/{tenant}/users")]
    [ApiController]
    public class OrganizationUnitUserController : CustomControllerBase
    {
        private readonly IOrganizationUnitUserService _organizationUnitUserService;
        private readonly ILogger<OrganizationUnitUserController> _logger;

        public OrganizationUnitUserController(
            IOrganizationUnitUserService organizationUnitUserService,
            ILogger<OrganizationUnitUserController> logger)
        {
            _organizationUnitUserService = organizationUnitUserService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all users in a specific organization unit by tenant slug
        /// </summary>
        /// <param name="tenant">The slug of the organization unit (tenant)</param>
        /// <returns>List of users in the organization unit</returns>
        /// <response code="200">List of users retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="404">Organization unit not found</response>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<OrganizationUnitUsersResponseDto>> GetUsersInOrganizationUnit(string tenant)
        {
            try
            {
                var users = await _organizationUnitUserService.GetUsersInOrganizationUnitAsync(tenant);
                var response = new OrganizationUnitUsersResponseDto
                {
                    Count = users.Count(),
                    Users = users
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetUsersInOrganizationUnit] Error getting users for tenant {tenant}");
                return StatusCode(500, new { message = "An error occurred while getting users." });
            }
        }

        /// <summary>
        /// Deletes a user from the organization unit by tenant slug
        /// </summary>
        /// <param name="tenant">The slug of the organization unit (tenant)</param>
        /// <param name="userId">The ID of the user to remove</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">User removed successfully</response>
        /// <response code="404">User or organization unit not found</response>
        [HttpDelete("{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string tenant, Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                if (userId == currentUserId)
                {
                    return BadRequest(new { message = "You cannot remove yourself from the organization unit." });
                }

                var deleted = await _organizationUnitUserService.DeleteUserAsync(tenant, userId);
                if (!deleted)
                    return NotFound(new { message = $"User with id '{userId}' not found in organization unit '{tenant}'." });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteUser] Error deleting user {userId} from tenant {tenant}");
                return StatusCode(500, new { message = "An error occurred while deleting user." });
            }
        }

        /// <summary>
        /// Gets all roles in a specific organization unit by tenant slug
        /// </summary>
        /// <param name="tenant">The slug of the organization unit (tenant)</param>
        /// <returns>List of roles in the organization unit</returns>
        /// <response code="200">List of roles retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="404">Organization unit not found</response>
        [HttpGet("roles")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AuthorityDto>>> GetRolesInOrganizationUnit(string tenant)
        {
            try
            {
                var roles = await _organizationUnitUserService.GetRolesInOrganizationUnitAsync(tenant);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetRolesInOrganizationUnit] Error getting roles for tenant {tenant}");
                return StatusCode(500, new { message = "An error occurred while getting roles." });
            }
        }
    }
}
