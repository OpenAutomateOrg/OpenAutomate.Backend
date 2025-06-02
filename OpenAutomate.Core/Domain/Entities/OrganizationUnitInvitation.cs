using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUnitInvitation : TenantEntity
    {
        [Required]
        public required string RecipientEmail { get; set; }
        public Guid InviterId { get; set; }
        [Required]
        public required string Token { get; set; }
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
