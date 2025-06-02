using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.OrganizationUnitInvitation;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using System.Security.Claims;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("{tenant}/api/organization-unit-invitation")]
    public class OrganizationUnitInvitationController : CustomControllerBase
    {
        private readonly IOrganizationUnitInvitationService _organizationUnitInvitationService;
        private readonly IOrganizationUnitService _organizationUnitService;

        public OrganizationUnitInvitationController(IOrganizationUnitInvitationService organizationUnitInvitationService, IOrganizationUnitService organizationUnitService)
        {
            _organizationUnitInvitationService = organizationUnitInvitationService;
            _organizationUnitService = organizationUnitService;
        }

        /// <summary>
        /// Sends an invitation to a user to join the specified organization.
        /// </summary>
        /// <param name="tenant">The organization slug.</param>
        /// <param name="request">The invitation request containing the recipient's email.</param>
        /// <returns>
        /// 200 OK: Returns the created invitation information.<br/>
        /// 404 Not Found: Organization not found.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> InviteUser([FromRoute] string tenant, [FromBody] InviteUserRequest request)
        {
            var org = await _organizationUnitService.GetOrganizationUnitBySlugAsync(tenant);
            if (org == null) return NotFound("Organization not found");
            var inviterId = GetCurrentUserId();
            try
            {
                var result = await _organizationUnitInvitationService.InviteUserAsync(org.Id, request, inviterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already a member of this organization") ||
                    ex.Message.Contains("There is already a pending invitation for this email"))
                {
                    return BadRequest(new { message = ex.Message });
                }
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Accepts an organization unit invitation using the provided token.
        /// </summary>
        /// <param name="request">The accept invitation request containing the invitation token.</param>
        /// <returns>
        /// 200 OK: Invitation accepted, returns success and invited email.<br/>
        /// 403 Forbidden: User is not invited to this organization unit.<br/>
        /// 404 Not Found: Invitation or user not found.<br/>
        /// 410 Gone: Invitation has expired.<br/>
        /// 500 Internal Server Error: Unknown error.
        /// </returns>
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            var userId = GetCurrentUserId();
            var invitation = await _organizationUnitInvitationService.GetInvitationByTokenAsync(request.Token);
            if (invitation == null)
                return NotFound(new { message = "Invitation not found" });

            var result = await _organizationUnitInvitationService.AcceptInvitationAsync(request.Token, userId);

            if (result != AcceptInvitationResult.Success)
            {
                if (result == AcceptInvitationResult.NotInvited)
                    return StatusCode(403, new { message = "You are not invited to this OU." });
                if (result == AcceptInvitationResult.InvitationNotFoundOrInvalid)
                    return NotFound(new { message = "Invitation not found or no longer valid" });
                if (result == AcceptInvitationResult.InvitationExpired)
                    return StatusCode(410, new { message = "Invitation has expired" });
                if (result == AcceptInvitationResult.UserNotFound)
                    return NotFound(new { message = "User not found" });

                return StatusCode(500, new { message = "Unknown error" });
            }

            return Ok(new { success = result, invitedEmail = invitation.RecipientEmail });
        }

        /// <summary>
        /// Checks if an email has a pending invitation to the specified organization.
        /// </summary>
        /// <param name="tenant">The organization slug.</param>
        /// <param name="email">The email address to check.</param>
        /// <returns>
        /// 200 OK: Returns invited status and invitation status if found.<br/>
        /// 404 Not Found: Organization not found.
        /// </returns>
        [HttpGet("check")]
        public async Task<IActionResult> CheckInvitation([FromRoute] string tenant, [FromQuery] string email)
        {
            var org = await _organizationUnitService.GetOrganizationUnitBySlugAsync(tenant);
            if (org == null) return NotFound("Organization not found");

            var invitation = await _organizationUnitInvitationService.GetPendingInvitationAsync(org.Id, email);
            if (invitation != null)
                return Ok(new { invited = true, status = invitation.Status.ToString() });

            return Ok(new { invited = false });
        }

        /// <summary>
        /// Checks the validity and status of an invitation token.
        /// </summary>
        /// <param name="token">The invitation token to check.</param>
        /// <returns>
        /// 200 OK: Returns invitation status, recipient email, expiration, and organization unit ID.<br/>
        /// 400 Bad Request: Token is required.<br/>
        /// 404 Not Found: Invitation not found or token invalid.
        /// </returns>
        [HttpGet("check-token")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckInvitationToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { status = "Invalid", message = "Token is required" });

            var invitation = await _organizationUnitInvitationService.GetInvitationByTokenAsync(token);

            if (invitation == null)
                return NotFound(new { status = "Invalid", message = "Invitation not found" });

            var status = invitation.Status.ToString();

            return Ok(new
            {
                status = status,
                recipientEmail = invitation.RecipientEmail,
                expiresAt = invitation.ExpiresAt,
                organizationUnitId = invitation.OrganizationUnitId
            });
        }
    }
}
