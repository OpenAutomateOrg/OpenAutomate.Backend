using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class AuthenticationResponse
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public SystemRole SystemRole { get; set; }
        public string Token { get; set; } = string.Empty;

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiration { get; set; }
    }
}
