using System;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public SystemRole SystemRole { get; set; }
    }
}