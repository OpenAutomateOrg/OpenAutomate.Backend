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
    }
}
