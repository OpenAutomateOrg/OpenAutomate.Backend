using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnitInvitation
{
    public class OrganizationUnitInvitationDto
    {
        public Guid Id { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Guid InviterId { get; set; }
        public Guid OrganizationUnitId { get; set; }
    }
}
