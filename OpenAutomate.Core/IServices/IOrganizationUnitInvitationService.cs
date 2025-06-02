using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.OrganizationUnitInvitation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationUnitInvitationService
    {
        Task<OrganizationUnitInvitationDto> InviteUserAsync(Guid organizationId, InviteUserRequest request, Guid inviterId);
        Task<AcceptInvitationResult> AcceptInvitationAsync(string token, Guid userId);
        Task<OrganizationUnitInvitation?> GetPendingInvitationAsync(Guid organizationId, string email);
        Task<OrganizationUnitInvitation> GetInvitationByTokenAsync(string token);
    }
    public enum AcceptInvitationResult
    {
        Success,
        InvitationNotFoundOrInvalid,
        InvitationExpired,
        UserNotFound,
        NotInvited
    }
}
