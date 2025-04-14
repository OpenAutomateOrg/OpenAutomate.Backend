using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/ou")]
    [Authorize]
    public class OrganizationUnitController : CustomControllerBase
    {
        private readonly IOrganizationUnitService _organizationUnitService;
        private readonly ILogger<OrganizationUnitController> _logger;

        public OrganizationUnitController(
            IOrganizationUnitService organizationUnitService,
            ILogger<OrganizationUnitController> logger)
        {
            _organizationUnitService = organizationUnitService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new organization unit
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrganizationUnitResponseDto>> CreateOrganizationUnit(CreateOrganizationUnitDto dto)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to create organization unit");
                    return Unauthorized();
                }
                
                var result = await _organizationUnitService.CreateOrganizationUnitAsync(dto, currentUser.Id);
                _logger.LogInformation("Organization unit created: {orgUnitId} by user {userId}", result.Id, currentUser.Id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating organization unit");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets all organization units the current user belongs to
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<OrganizationUnitResponseDto>>> GetMyOrganizationUnits()
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to get user's organization units");
                    return Unauthorized();
                }
                
                var result = await _organizationUnitService.GetUserOrganizationUnitsAsync(currentUser.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's organization units");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets an organization unit by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetOrganizationUnit(Guid id)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to get organization unit");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to access unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                var result = await _organizationUnitService.GetOrganizationUnitByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets an organization unit by slug
        /// </summary>
        [HttpGet("by-slug/{slug}")]
        public async Task<ActionResult<OrganizationUnitResponseDto>> GetOrganizationUnitBySlug(string slug)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to get organization unit by slug");
                    return Unauthorized();
                }
                
                var orgUnit = await _organizationUnitService.GetOrganizationUnitBySlugAsync(slug);
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(orgUnit.Id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to access unauthorized organization unit {slug}", 
                        currentUser.Id, slug);
                    return Forbid();
                }
                
                return Ok(orgUnit);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found by slug");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization unit by slug");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Checks the impact of changing an organization unit's name
        /// </summary>
        [HttpGet("{id}/name-change-impact")]
        public async Task<ActionResult<SlugChangeWarningDto>> CheckNameChangeImpact(Guid id, [FromQuery] string newName)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to check name change impact");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to check name change impact for unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                var result = await _organizationUnitService.CheckNameChangeImpactAsync(id, newName);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found when checking name change impact");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking name change impact");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Updates an organization unit
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<OrganizationUnitResponseDto>> UpdateOrganizationUnit(Guid id, UpdateOrganizationUnitDto dto)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to update organization unit");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to update unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                var result = await _organizationUnitService.UpdateOrganizationUnitAsync(id, dto);
                _logger.LogInformation("Organization unit updated: {orgUnitId} by user {userId}", id, currentUser.Id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found when updating");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when updating organization unit");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Deactivates (soft deletes) an organization unit
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeactivateOrganizationUnit(Guid id)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to deactivate organization unit");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to deactivate unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                await _organizationUnitService.DeactivateOrganizationUnitAsync(id);
                _logger.LogInformation("Organization unit deactivated: {orgUnitId} by user {userId}", id, currentUser.Id);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found when deactivating");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets all users in an organization unit
        /// </summary>
        [HttpGet("{id}/users")]
        public async Task<ActionResult<IEnumerable<OrganizationUnitUserDto>>> GetOrganizationUnitUsers(Guid id)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to get organization unit users");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to access users for unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                var result = await _organizationUnitService.GetOrganizationUnitUsersAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Organization unit not found when getting users");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization unit users");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Adds a user to an organization unit
        /// </summary>
        [HttpPost("{id}/users")]
        public async Task<ActionResult> AddUserToOrganizationUnit(Guid id, AddUserToOrganizationUnitDto dto)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to add user to organization unit");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to add user to unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                await _organizationUnitService.AddUserToOrganizationUnitAsync(id, dto.UserId, dto.Role);
                _logger.LogInformation("User {newUserId} added to organization unit {orgUnitId} by user {userId}", 
                    dto.UserId, id, currentUser.Id);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found when adding user to organization unit");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Removes a user from an organization unit
        /// </summary>
        [HttpDelete("{id}/users/{userId}")]
        public async Task<ActionResult> RemoveUserFromOrganizationUnit(Guid id, Guid userId)
        {
            try
            {
                if (currentUser == null)
                {
                    _logger.LogWarning("Unauthorized attempt to remove user from organization unit");
                    return Unauthorized();
                }
                
                // Check if user has access to the organization unit
                if (!await _organizationUnitService.UserHasAccessToOrganizationUnitAsync(id, currentUser.Id))
                {
                    _logger.LogWarning("User {userId} attempted to remove user from unauthorized organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return Forbid();
                }
                
                // Prevent users from removing themselves
                if (userId == currentUser.Id)
                {
                    _logger.LogWarning("User {userId} attempted to remove themselves from organization unit {orgUnitId}", 
                        currentUser.Id, id);
                    return BadRequest("You cannot remove yourself from an organization unit");
                }
                
                await _organizationUnitService.RemoveUserFromOrganizationUnitAsync(id, userId);
                _logger.LogInformation("User {removedUserId} removed from organization unit {orgUnitId} by user {userId}", 
                    userId, id, currentUser.Id);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found when removing user from organization unit");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when removing user from organization unit");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from organization unit");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
} 