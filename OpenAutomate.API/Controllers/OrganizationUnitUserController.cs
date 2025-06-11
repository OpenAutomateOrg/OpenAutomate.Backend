using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [Route("api/ou/{tenant}/users")]
    [ApiController]
    public class OrganizationUnitUserController : CustomControllerBase
    {
        private readonly IOrganizationUnitUserService _organizationUnitUserService;

        public OrganizationUnitUserController(IOrganizationUnitUserService organizationUnitUserService)
        {
            _organizationUnitUserService = organizationUnitUserService;
        }

        /// <summary>
        /// Gets all users in a specific organization unit by tenant slug
        /// </summary>
        /// <param name="tenant">The slug of the organization unit (tenant)</param>
        /// <returns>List of users in the organization unit</returns>
        /// <response code="200">List of users retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Organization unit not found</response>
        [HttpGet]
        [RequirePermission(Resources.UserResource, Permissions.View)]
        public async Task<ActionResult<OrganizationUnitUsersResponseDto>> GetUsersInOrganizationUnit(string tenant)
        {
            var users = await _organizationUnitUserService.GetUsersInOrganizationUnitAsync(tenant);
            var response = new OrganizationUnitUsersResponseDto
            {
                Count = users.Count(),
                Users = users
            };
            return Ok(response);
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
        [RequirePermission(Resources.UserResource, Permissions.Delete)]
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
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while removing the user from the organization unit.");
            }
        }
    }
}
