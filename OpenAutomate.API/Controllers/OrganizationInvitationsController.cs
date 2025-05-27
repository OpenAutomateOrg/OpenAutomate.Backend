using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.OrganizationInvitation;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using System.Security.Claims;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("{tenant}/api/organization-invitations")]
    public class OrganizationInvitationsController : CustomControllerBase
    {
        private readonly IOrganizationInvitationService _organizationInvitationService;
        private readonly IOrganizationUnitService _organizationUnitService;

        public OrganizationInvitationsController(IOrganizationInvitationService organizationInvitationService, IOrganizationUnitService organizationUnitService)
        {
            _organizationInvitationService = organizationInvitationService;
            _organizationUnitService = organizationUnitService;
        }

        [HttpPost]
        public async Task<IActionResult> InviteUser([FromRoute] string tenant, [FromBody] InviteUserRequest request)
        {
            var org = await _organizationUnitService.GetOrganizationUnitBySlugAsync(tenant);
            if (org == null) return NotFound("Organization not found");
            var inviterId = GetCurrentUserId();
            var result = await _organizationInvitationService.InviteUserAsync(org.Id, request, inviterId);
            return Ok(result);
        }

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _organizationInvitationService.AcceptInvitationAsync(request.Token, userId);
            return Ok(new { success = result });
        }
    }
}
