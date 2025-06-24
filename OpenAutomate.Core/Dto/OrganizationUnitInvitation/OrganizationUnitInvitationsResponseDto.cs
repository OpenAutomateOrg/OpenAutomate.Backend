using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnitInvitation
{
    public class OrganizationUnitInvitationsResponseDto
    {
        public int Count { get; set; }
        public List<OrganizationUnitInvitationDto> Invitations { get; set; } = new();
    }
}
