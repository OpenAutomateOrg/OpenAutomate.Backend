using OpenAutomate.Core.Dto.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnitUser
{
    public class OrganizationUnitUsersResponseDto
    {
        public int Count { get; set; }
        public IEnumerable<OrganizationUnitUserDetailDto> Users { get; set; } = new List<OrganizationUnitUserDetailDto>();
    }
}
