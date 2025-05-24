using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.InvitationDto
{
    public class AcceptInvitationRequestDto
    {
        [Required(ErrorMessage = "Token là bắt buộc")]
        public string Token { get; set; }
    }
} 