using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.UserDto
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
} 