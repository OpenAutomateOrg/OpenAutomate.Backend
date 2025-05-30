using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.OrganizationInvitation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationInvitationService
    {
        Task<OrganizationInvitationDto> InviteUserAsync(Guid organizationId, InviteUserRequest request, Guid inviterId);
        Task<AcceptInvitationResult> AcceptInvitationAsync(string token, Guid userId);
        Task<OrganizationInvitation?> GetPendingInvitationAsync(Guid organizationId, string email);
        Task<OrganizationInvitation> GetInvitationByTokenAsync(string token);
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
