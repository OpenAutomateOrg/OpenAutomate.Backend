using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnitInvitation
{
    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
