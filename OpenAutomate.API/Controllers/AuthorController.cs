using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenAutomate.Core.Domain.IRepository;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing user authorities and permissions within an organization unit
    /// </summary>
    /// <remarks>
    /// Provides endpoints for creating roles, assigning permissions, and managing user authorities.
    /// All operations are scoped to the current organization unit (tenant).
    /// Permission levels: 0=No Access, 1=View, 2=Create, 3=Update, 4=Delete/Full Admin
    /// </remarks>
    [ApiController]
    [Route("{tenant}/api/author")]
    [Authorize]
    public class AuthorController : CustomControllerBase
    {
        private readonly IAuthorizationManager _authorizationManager;
        private readonly IOrganizationUnitService _organizationUnitService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorController"/> class
        /// </summary>
        /// <param name="authorizationManager">The authorization manager service</param>
        /// <param name="organizationUnitService">The organization unit service</param>
        /// <param name="cacheInvalidationService">The cache invalidation service</param>
        /// <param name="tenantContext">The tenant context</param>
        public AuthorController(
            IAuthorizationManager authorizationManager, 
            IOrganizationUnitService organizationUnitService,
            ICacheInvalidationService cacheInvalidationService,
            ITenantContext tenantContext)
        {
            _authorizationManager = authorizationManager;
            _organizationUnitService = organizationUnitService;
            _cacheInvalidationService = cacheInvalidationService;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Creates a new authority (role) within the organization unit
        /// Only OWNER can create new authorities
        /// </summary>
        /// <param name="dto">The authority creation details</param>
        /// <returns>The created authority details</returns>
        [HttpPost("authority")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAuthority([FromBody] CreateAuthorityDto dto)
        {
            try
            {
                var result = await _authorizationManager.CreateAuthorityAsync(dto);
                
                // Invalidate tenant permissions cache and API response cache
                if (_tenantContext.HasTenant)
                {
                    await _cacheInvalidationService.InvalidateTenantPermissionsCacheAsync(_tenantContext.CurrentTenantId);
                    await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/Roles", _tenantContext.CurrentTenantId);
                }
                
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets all authorities within the current organization unit
        /// </summary>
        /// <returns>List of authorities with their permissions</returns>
        [HttpGet("authorities")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllAuthorities()
        {
            var authorities = await _authorizationManager.GetAllAuthoritiesWithPermissionsAsync();
            return Ok(authorities);
        }

        /// <summary>
        /// Gets a specific authority by ID with its permissions
        /// </summary>
        /// <param name="authorityId">The authority ID</param>
        /// <returns>Authority details with permissions</returns>
        [HttpGet("authority/{authorityId}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuthority(Guid authorityId)
        {
            var authority = await _authorizationManager.GetAuthorityWithPermissionsAsync(authorityId);
            if (authority == null)
                return NotFound();

            return Ok(authority);
        }

        /// <summary>
        /// Updates an existing authority's details and permissions
        /// Only OWNER can update authorities
        /// </summary>
        /// <param name="authorityId">The authority ID</param>
        /// <param name="dto">Updated authority details</param>
        /// <returns>Success response</returns>
        [HttpPut("authority/{authorityId}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAuthority(Guid authorityId, [FromBody] UpdateAuthorityDto dto)
        {
            try
            {
                await _authorizationManager.UpdateAuthorityAsync(authorityId, dto);
                
                // Invalidate tenant permissions cache and API response cache
                if (_tenantContext.HasTenant)
                {
                    await _cacheInvalidationService.InvalidateTenantPermissionsCacheAsync(_tenantContext.CurrentTenantId);
                    await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/Roles", _tenantContext.CurrentTenantId);
                }
                
                return Ok();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot modify"))
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an authority (role) from the organization unit
        /// Only OWNER can delete authorities. Cannot delete system authorities (OWNER, etc.)
        /// </summary>
        /// <param name="authorityId">The authority ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("authority/{authorityId}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteAuthority(Guid authorityId)
        {
            try
            {
                await _authorizationManager.DeleteAuthorityAsync(authorityId);
                
                // Invalidate tenant permissions cache and API response cache
                if (_tenantContext.HasTenant)
                {
                    await _cacheInvalidationService.InvalidateTenantPermissionsCacheAsync(_tenantContext.CurrentTenantId);
                    await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/Roles", _tenantContext.CurrentTenantId);
                }
                
                return Ok();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
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
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserAuthorities(Guid userId)
        {
            var authorities = await _authorizationManager.GetUserAuthoritiesAsync(userId);
            var result = authorities.Select(a => new AuthorityDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description
            });
            return Ok(result);
        }

        /// <summary>
        /// Assigns an authority to a user
        /// Requires UPDATE permission on OrganizationUnit
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="dto">The authority assignment details containing the authority ID</param>
        /// <returns>A success response if the assignment is successful</returns>
        /// <response code="200">Authority successfully assigned to user</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">User or authority not found</response>
        [HttpPost("user/{userId}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignAuthorityToUser(Guid userId, [FromBody] AssignAuthorityDto dto)
        {
            await _authorizationManager.AssignAuthorityToUserAsync(userId, dto.AuthorityId);

            // Cache invalidation is handled by AuthorizationManagerCachingDecorator

            return Ok();
        }

        /// <summary>
        /// Removes an authority from a user
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="authorityId">The ID of the authority to remove</param>
        /// <returns>A success response if the removal is successful</returns>
        /// <response code="200">Authority successfully removed from user</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">User or authority assignment not found</response>
        [HttpDelete("user/{userId}/authority/{authorityId}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAuthorityFromUser(Guid userId, Guid authorityId)
        {
            await _authorizationManager.RemoveAuthorityFromUserAsync(userId, authorityId);

            // Cache invalidation is handled by AuthorizationManagerCachingDecorator

            return Ok();
        }

        /// <summary>
        /// Gets all available resources and their permission levels
        /// Used for role creation UI
        /// </summary>
        /// <returns>List of resources with available permission levels</returns>
        [HttpGet("resources")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAvailableResources()
        {
            var resources = await _authorizationManager.GetAvailableResourcesAsync();
            return Ok(resources);
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
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Create)]
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
            
            // Invalidate tenant permissions cache and API response cache
            if (_tenantContext.HasTenant)
            {
                await _cacheInvalidationService.InvalidateTenantPermissionsCacheAsync(_tenantContext.CurrentTenantId);
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/Roles", _tenantContext.CurrentTenantId);
            }
            
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
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveResourcePermission(string authorityName, string resourceName)
        {
            await _authorizationManager.RemoveResourcePermissionAsync(authorityName, resourceName);
            
            // Invalidate tenant permissions cache and API response cache
            if (_tenantContext.HasTenant)
            {
                await _cacheInvalidationService.InvalidateTenantPermissionsCacheAsync(_tenantContext.CurrentTenantId);
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/Roles", _tenantContext.CurrentTenantId);
            }
            
            return Ok();
        }

        /// <summary>
        /// Assigns multiple authorities (roles) to a user in one request
        /// </summary>
        /// <param name="tenant">The tenant slug</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="dto">The authority assignment details containing the list of authority IDs</param>
        /// <returns>A success response if the assignment is successful</returns>
        /// <response code="200">Authorities successfully assigned to user</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">User or authority not found</response>
        [HttpPost("user/{userId}/assign-multiple-roles")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignAuthoritiesToUserBulk(string tenant, Guid userId, [FromBody] AssignAuthoritiesDto dto)
        {
            var ou = await _organizationUnitService.GetOrganizationUnitBySlugAsync(tenant);
            if (ou == null)
                return NotFound(new { message = $"Organization unit '{tenant}' not found." });
            await _authorizationManager.AssignAuthoritiesToUserAsync(userId, dto.AuthorityIds, ou.Id);

            // Cache invalidation is handled by AuthorizationManagerCachingDecorator

            return Ok();
        }

        /// <summary>
        /// Test endpoint to check user permissions immediately after role assignment
        /// </summary>
        /// <param name="userId">The user ID to check permissions for</param>
        /// <param name="resourceName">The resource name to check</param>
        /// <param name="permission">The permission level to check</param>
        /// <returns>Permission check result with cache status</returns>
        [HttpGet("user/{userId}/test-permission")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TestUserPermission(Guid userId, [FromQuery] string resourceName, [FromQuery] int permission)
        {
            var hasPermission = await _authorizationManager.HasPermissionAsync(userId, resourceName, permission);
            var userAuthorities = await _authorizationManager.GetUserAuthoritiesAsync(userId);

            return Ok(new
            {
                UserId = userId,
                ResourceName = resourceName,
                Permission = permission,
                HasPermission = hasPermission,
                UserAuthorities = userAuthorities.Select(a => new { a.Id, a.Name }),
                Timestamp = DateTime.UtcNow,
                TenantId = _tenantContext.CurrentTenantId
            });
        }
    }
}