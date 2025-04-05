using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Domain.Dto.UserDto
{
    public class AuthenticationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
} 