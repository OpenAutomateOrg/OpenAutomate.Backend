using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUnitInvitation : TenantEntity
    {
        public Guid OrganizationUnitId { get; set; }
        public virtual OrganizationUnit OrganizationUnit { get; set; }

        public string RecipientEmail { get; set; }

        public Guid InviterId { get; set; }
        public virtual User Inviter { get; set; }

        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public InvitationStatus Status { get; set; }
    }

    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Expired,
        Revoked
    }
}
