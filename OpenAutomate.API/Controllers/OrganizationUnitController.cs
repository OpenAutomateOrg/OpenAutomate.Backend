using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing organization units (tenants) in the system
    /// </summary>
    /// <remarks>
    /// Provides endpoints for creating, retrieving, and updating organization units.
    /// Some endpoints require specific permissions.
    /// </remarks>
    [Route("api/ou")]
    [ApiController]
    [Authorize]
    public class OrganizationUnitController : CustomControllerBase
    {
        private readonly IOrganizationUnitService _organizationUnitService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<OrganizationUnitController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationUnitController"/> class
        /// </summary>
        /// <param name="organizationUnitService">The organization unit service</param>
        /// <param name="cacheInvalidationService">The cache invalidation service</param>
        /// <param name="logger">The logger</param>
        public OrganizationUnitController(
            IOrganizationUnitService organizationUnitService,
            ICacheInvalidationService cacheInvalidationService,
            ILogger<OrganizationUnitController> logger)
        {
            _organizationUnitService = organizationUnitService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new organization unit with default authorities
        /// </summary>
        /// <param name="dto">The organization unit creation data</param>
        /// <returns>The newly CreatedAtorganization unit details</returns>
        /// <remarks>
        /// This endpoint creates a new organization unit (tenant) and automatically sets up
        /// default authorities (OWNER, MANAGER, DEVELOPER, USER). The authenticated user
        /// will be assigned as an OWNER of the new organization unit.
        /// 
        /// The slug is automatically generated from the name and does not need to be provided in the request.
        /// </remarks>
        /// <response code="201">Organization unit successfully created</response>
        /// <response code="400">Invalid organization unit data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error during creation process</response>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> Create([FromBody] CreateOrganizationUnitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get the current user ID from the claims
                var userId = GetCurrentUserId();
                
                var result = await _organizationUnitService.CreateOrganizationUnitAsync(dto, userId);
                
                // Invalidate caches related to subscription and user profile after creating a new organization unit
                // This ensures the frontend receives fresh trial status data immediately
                
                _logger.LogInformation("Invalidating caches for new organization unit {OrganizationUnitId} with slug {Slug}", result.Id, result.Slug);
                
                // CRITICAL: Invalidate tenant resolution cache - this is the key that was causing the issue
                // The tenant resolution cache stores tenant data including subscription info
                await _cacheInvalidationService.InvalidateTenantResolutionCacheAsync(result.Slug);
                
                // Also invalidate any cached subscription API responses for this tenant
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/api/subscription", result.Id);
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/api/subscription/status", result.Id);
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/api/account/profile");
                
                // Clear broader cache patterns to catch any variations
                await _cacheInvalidationService.InvalidateCachePatternAsync($"*tenant*{result.Slug.ToLowerInvariant()}*");
                await _cacheInvalidationService.InvalidateCachePatternAsync($"*{result.Id}*");
                
                _logger.LogInformation("Cache invalidation completed for organization unit {OrganizationUnitId} with slug {Slug}", result.Id, result.Slug);
                
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the organization unit: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all organization units that the current user belongs to
        /// </summary>
        /// <returns>A collection of organization units the user belongs to and the total count</returns>
        /// <remarks>
        /// This endpoint retrieves all organization units that the authenticated user belongs to,
        /// regardless of their role (OWNER, MANAGER, etc.) within those units.
        /// </remarks>
        /// <response code="200">Organization units retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error during retrieval process</response>
        [HttpGet("my-ous")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserOrganizationUnitsResponseDto>> GetMyOrganizationUnits()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _organizationUnitService.GetUserOrganizationUnitsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving your organization units: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an organization unit by its unique identifier
        /// </summary>
        /// <param name="id">The unique identifier of the organization unit</param>
        /// <returns>The organization unit details</returns>
        /// <response code="200">Organization unit found and returned</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Organization unit not found</response>
        [HttpGet("{id}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetById(Guid id)
        {
            var organizationUnit = await _organizationUnitService.GetOrganizationUnitByIdAsync(id);
            if (organizationUnit == null)
                return NotFound();

            return Ok(organizationUnit);
        }

        /// <summary>
        /// Gets an organization unit by its URL-friendly slug
        /// </summary>
        /// <param name="slug">The URL-friendly identifier (slug) of the organization unit</param>
        /// <returns>The organization unit details</returns>
        /// <response code="200">Organization unit found and returned</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Organization unit not found</response>
        [HttpGet("slug/{slug}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetBySlug(string slug)
        {
            var organizationUnit = await _organizationUnitService.GetOrganizationUnitBySlugAsync(slug);
            if (organizationUnit == null)
                return NotFound();

            return Ok(organizationUnit);
        }

        /// <summary>
        /// Gets all organization units in the system
        /// </summary>
        /// <returns>A collection of all organization units</returns>
        /// <remarks>
        /// This endpoint retrieves all organization units that the user has permission to view.
        /// For regular users, this will include only the organization units they belong to.
        /// For administrators, this may include all organization units in the system.
        /// </remarks>
        /// <response code="200">List of organization units retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        [HttpGet]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<OrganizationUnitResponseDto>>> GetAll()
        {
            var organizationUnits = await _organizationUnitService.GetAllOrganizationUnitsAsync();
            return Ok(organizationUnits);
        }

        /// <summary>
        /// Updates an existing organization unit
        /// </summary>
        /// <param name="id">The unique identifier of the organization unit to update</param>
        /// <param name="dto">The updated organization unit data</param>
        /// <returns>The updated organization unit details</returns>
        /// <remarks>
        /// This endpoint updates organization unit properties such as name and description.
        /// The slug is automatically regenerated when the name changes.
        /// Note that changing the name will impact URL routing to this organization unit as the slug will change.
        /// </remarks>
        /// <response code="200">Organization unit updated successfully</response>
        /// <response code="400">Invalid organization unit data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Organization unit not found</response>
        /// <response code="500">Server error during update process</response>
        [HttpPut("{id}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> Update(Guid id, [FromBody] CreateOrganizationUnitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var organizationUnit = await _organizationUnitService.GetOrganizationUnitByIdAsync(id);
                if (organizationUnit == null)
                    return NotFound();

                var oldSlug = organizationUnit.Slug;
                var result = await _organizationUnitService.UpdateOrganizationUnitAsync(id, dto);
                
                // Invalidate tenant resolution cache for both old and new slugs
                await _cacheInvalidationService.InvalidateTenantResolutionCacheAsync(oldSlug);
                if (result.Slug != oldSlug)
                {
                    await _cacheInvalidationService.InvalidateTenantResolutionCacheAsync(result.Slug);
                }
                
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the organization unit: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks the potential impact of changing an organization unit's name
        /// </summary>
        /// <param name="id">The unique identifier of the organization unit</param>
        /// <param name="newName">The proposed new name for the organization unit</param>
        /// <returns>A warning object containing information about the impact of the name change</returns>
        /// <remarks>
        /// This endpoint analyzes the impact of a name change, particularly on the slug (URL-friendly identifier).
        /// It helps users understand potential routing or access issues before committing to a name change.
        /// </remarks>
        /// <response code="200">Impact analysis completed successfully</response>
        /// <response code="400">New name is invalid or missing</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="404">Organization unit not found</response>
        /// <response code="500">Server error during analysis process</response>
        [HttpGet("{id}/check-name-change")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SlugChangeWarningDto>> CheckNameChange(Guid id, [FromQuery] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("New name is required");

            try
            {
                var organizationUnit = await _organizationUnitService.GetOrganizationUnitByIdAsync(id);
                if (organizationUnit == null)
                    return NotFound();

                var result = await _organizationUnitService.CheckNameChangeImpactAsync(id, newName);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while checking name change impact: {ex.Message}");
            }
        }

        /// <summary>
        /// Request deletion of organization unit (Owner only)
        /// </summary>
        [HttpPost("{id}/request-deletion")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        public async Task<ActionResult<DeletionRequestDto>> RequestDeletion(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _organizationUnitService.RequestDeletionAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Organization unit not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancel pending deletion
        /// </summary>
        [HttpPost("{id}/cancel-deletion")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        public async Task<ActionResult<DeletionRequestDto>> CancelDeletion(Guid id)
        {
            try
            {
                var result = await _organizationUnitService.CancelDeletionAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Organization unit not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Get deletion status with countdown
        /// </summary>
        [HttpGet("{id}/deletion-status")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Delete)]
        public async Task<ActionResult<DeletionStatusDto>> GetDeletionStatus(Guid id)
        {
            try
            {
                var result = await _organizationUnitService.GetDeletionStatusAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Organization unit not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
} 