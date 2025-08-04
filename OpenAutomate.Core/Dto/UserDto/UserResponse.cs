using System;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; } = false;
        public SystemRole SystemRole { get; set; } = SystemRole.User;
        public DateTime? CreatedAt { get; set; }
    }
}