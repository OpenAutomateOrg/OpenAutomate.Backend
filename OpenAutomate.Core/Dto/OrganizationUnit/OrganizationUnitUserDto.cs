using System;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class OrganizationUnitUserDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
} 