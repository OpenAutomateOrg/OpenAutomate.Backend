using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.InvitationDto
{
    public class SendInvitationRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        
        public string Name { get; set; }
        
        [Required(ErrorMessage = "OrganizationId là bắt buộc")]
        public Guid OrganizationId { get; set; }
    }
} 