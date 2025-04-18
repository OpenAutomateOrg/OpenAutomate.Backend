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
    public class OrganizationUnitController : CustomControllerBase
    {
        private readonly IOrganizationUnitService _organizationUnitService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationUnitController"/> class
        /// </summary>
        /// <param name="organizationUnitService">The organization unit service</param>
        public OrganizationUnitController(IOrganizationUnitService organizationUnitService)
        {
            _organizationUnitService = organizationUnitService;
        }

        /// <summary>
        /// Creates a new organization unit with default authorities
        /// </summary>
        /// <param name="dto">The organization unit creation data</param>
        /// <returns>The newly created organization unit details</returns>
        /// <remarks>
        /// This endpoint creates a new organization unit (tenant) and automatically sets up
        /// default authorities (OWNER, MANAGER, DEVELOPER, USER). The authenticated user
        /// will be assigned as an OWNER of the new organization unit.
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
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the organization unit: {ex.Message}");
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
        /// This endpoint updates organization unit properties such as name, description, and slug.
        /// Note that changing the slug may impact URL routing to this organization unit.
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

                var result = await _organizationUnitService.UpdateOrganizationUnitAsync(id, dto);
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
    }
} 