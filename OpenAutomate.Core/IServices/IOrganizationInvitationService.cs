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
        Task<bool> AcceptInvitationAsync(string token, Guid userId);
    }
}
