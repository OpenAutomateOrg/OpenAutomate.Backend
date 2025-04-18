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
    [Route("api/ou")]
    [ApiController]
    public class OrganizationUnitController : CustomControllerBase
    {
        private readonly IOrganizationUnitService _organizationUnitService;

        public OrganizationUnitController(IOrganizationUnitService organizationUnitService)
        {
            _organizationUnitService = organizationUnitService;
        }

        /// <summary>
        /// Creates a new organization unit with default authorities (OWNER, MANAGER, DEVELOPER, USER)
        /// </summary>
        [HttpPost("create")]
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
        /// Gets an organization unit by its ID
        /// </summary>
        [HttpGet("{id}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetById(Guid id)
        {
            var organizationUnit = await _organizationUnitService.GetOrganizationUnitByIdAsync(id);
            if (organizationUnit == null)
                return NotFound();

            return Ok(organizationUnit);
        }

        /// <summary>
        /// Gets an organization unit by its slug
        /// </summary>
        [HttpGet("slug/{slug}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetBySlug(string slug)
        {
            var organizationUnit = await _organizationUnitService.GetOrganizationUnitBySlugAsync(slug);
            if (organizationUnit == null)
                return NotFound();

            return Ok(organizationUnit);
        }

        /// <summary>
        /// Gets all organization units
        /// </summary>
        [HttpGet]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        public async Task<ActionResult<IEnumerable<OrganizationUnitResponseDto>>> GetAll()
        {
            var organizationUnits = await _organizationUnitService.GetAllOrganizationUnitsAsync();
            return Ok(organizationUnits);
        }

        /// <summary>
        /// Updates an organization unit
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
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
        [HttpGet("{id}/check-name-change")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.Update)]
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