using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class AddUserToOrganizationUnitDto
    {
        [Required]
        public Guid UserId { get; set; }
        
        // For future role-based implementation
        public string Role { get; set; } = "Member";
    }
} 