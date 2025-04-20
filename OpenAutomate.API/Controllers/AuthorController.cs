using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing user authorities and permissions
    /// </summary>
    /// <remarks>
    /// Provides endpoints for assigning, retrieving, and removing authorities and permissions.
    /// All endpoints require authentication and specific permissions.
    /// </remarks>
    [ApiController]
    [Route("{tenant}/api/author")]
    [Authorize]
    public class AuthorController : CustomControllerBase
    {
        private readonly IAuthorizationManager _authorizationManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorController"/> class
        /// </summary>
        /// <param name="authorizationManager">The authorization manager service</param>
        public AuthorController(IAuthorizationManager authorizationManager)
        {
            _authorizationManager = authorizationManager;
        }
        
        /// <summary>
        /// Gets all authorities assigned to a specific user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>A collection of authority names assigned to the user</returns>
        /// <response code="200">Authorities successfully retrieved</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        [HttpGet("user/{userId}")]
        [RequirePermission(Resources.AdminResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserAuthorities(Guid userId)
        {
            var authorities = await _authorizationManager.GetUserAuthoritiesAsync(userId);
            var result = authorities.Select(a => new AuthorityDto { Name = a.Name });
            return Ok(result);
        }
        
        /// <summary>
        /// Assigns an authority to a user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="dto">The authority assignment details containing the authority name</param>
        /// <returns>A success response if the assignment is successful</returns>
        /// <response code="200">Authority successfully assigned to user</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">User or authority not found</response>
        [HttpPost("user/{userId}")]
        [RequirePermission(Resources.AdminResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignAuthorityToUser(Guid userId, [FromBody] AssignAuthorityDto dto)
        {
            await _authorizationManager.AssignAuthorityToUserAsync(userId, dto.AuthorityName);
            return Ok();
        }
        
        /// <summary>
        /// Removes an authority from a user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="authorityName">The name of the authority to remove</param>
        /// <returns>A success response if the removal is successful</returns>
        /// <response code="200">Authority successfully removed from user</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">User or authority assignment not found</response>
        [HttpDelete("user/{userId}/{authorityName}")]
        [RequirePermission(Resources.AdminResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAuthorityFromUser(Guid userId, string authorityName)
        {
            await _authorizationManager.RemoveAuthorityFromUserAsync(userId, authorityName);
            return Ok();
        }
        
        /// <summary>
        /// Adds a resource permission to an authority
        /// </summary>
        /// <param name="dto">The resource permission details containing authority name, resource name, and permission</param>
        /// <returns>A success response if the permission is successfully added</returns>
        /// <response code="200">Resource permission successfully added</response>
        /// <response code="400">Invalid resource permission data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Authority or resource not found</response>
        [HttpPost("permission")]
        [RequirePermission(Resources.AdminResource, Permissions.Create)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddResourcePermission([FromBody] ResourcePermissionDto dto)
        {
            await _authorizationManager.AddResourcePermissionAsync(
                dto.AuthorityName,
                dto.ResourceName,
                dto.Permission
            );
            return Ok();
        }
        
        /// <summary>
        /// Removes all permissions for a resource from an authority
        /// </summary>
        /// <param name="authorityName">The name of the authority</param>
        /// <param name="resourceName">The name of the resource</param>
        /// <returns>A success response if the permission is successfully removed</returns>
        /// <response code="200">Resource permissions successfully removed</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Authority, resource, or permission not found</response>
        [HttpDelete("permission/{authorityName}/{resourceName}")]
        [RequirePermission(Resources.AdminResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveResourcePermission(string authorityName, string resourceName)
        {
            await _authorizationManager.RemoveResourcePermissionAsync(authorityName, resourceName);
            return Ok();
        }
    }
} 