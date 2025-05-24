using System;

namespace OpenAutomate.Core.Dto.InvitationDto
{
    public class InvitationResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string InviterName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
} 