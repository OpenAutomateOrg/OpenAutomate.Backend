using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class AuthenticationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}