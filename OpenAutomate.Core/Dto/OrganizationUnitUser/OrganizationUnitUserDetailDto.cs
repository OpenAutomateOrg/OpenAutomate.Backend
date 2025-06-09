using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnitUser
{
    public class OrganizationUnitUserDetailDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Role { get; set; } = string.Empty;
        public DateTime? JoinedAt { get; set; }
    }
}
