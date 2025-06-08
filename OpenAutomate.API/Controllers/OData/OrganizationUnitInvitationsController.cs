using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.Core.Dto.OrganizationUnitInvitation;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Organization Unit Invitations
    /// </summary>
    [Route("{tenant}/odata/OrganizationUnitInvitations")]
    [ApiController]
    [Authorize]
    public class OrganizationUnitInvitationsController : ODataController
    {
        private readonly IOrganizationUnitInvitationService _invitationService;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<OrganizationUnitInvitationsController> _logger;

        public OrganizationUnitInvitationsController(
            IOrganizationUnitInvitationService invitationService,
            ITenantContext tenantContext,
            ILogger<OrganizationUnitInvitationsController> logger)
        {
            _invitationService = invitationService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all Organization Unit Invitations with OData query support
        /// </summary>
        /// <returns>Collection of OrganizationUnitInvitationDto</returns>
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                string? tenantSlug = _tenantContext.CurrentTenantSlug;
                if (string.IsNullOrEmpty(tenantSlug))
                {
                    tenantSlug = RouteData.Values["tenant"]?.ToString();
                }
                if (string.IsNullOrEmpty(tenantSlug))
                {
                    _logger.LogError("Tenant slug not available in context or route data");
                    return BadRequest("Tenant not specified");
                }

                var resolved = await _tenantContext.ResolveTenantFromSlugAsync(tenantSlug);
                if (!resolved)
                {
                    _logger.LogError("Tenant not found for slug: {TenantSlug}", tenantSlug);
                    return NotFound($"Tenant '{tenantSlug}' not found");
                }

                var invitations = await _invitationService.ListInvitationsByOrganizationUnitAsync(_tenantContext.CurrentTenantId);
                return Ok(invitations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization unit invitations for OData query");
                return StatusCode(500, "An error occurred while retrieving organization unit invitations");
            }
        }
    }
}