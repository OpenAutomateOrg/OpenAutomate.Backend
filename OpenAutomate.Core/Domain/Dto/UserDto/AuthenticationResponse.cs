using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAutomate.Domain.Dto.UserDto
{
    public class AuthenticationResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        
        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }
    }
}
