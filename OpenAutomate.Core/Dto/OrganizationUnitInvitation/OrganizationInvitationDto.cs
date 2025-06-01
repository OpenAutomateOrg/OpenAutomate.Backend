using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationInvitation
{
    public class OrganizationInvitationDto
    {
        public Guid Id { get; set; }
        public string RecipientEmail { get; set; }
        public string Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Guid InviterId { get; set; }
        public Guid OrganizationUnitId { get; set; }
    }
}
