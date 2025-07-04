using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Dto.OrganizationUnit;

namespace OpenAutomate.Core.Dto.UserDto
{
    /// <summary>
    /// Complete user profile with permissions across all organization units
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// User's unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's system-level role (Admin or User)
        /// </summary>
        public SystemRole SystemRole { get; set; }

        /// <summary>
        /// All organization units the user belongs to with their permissions
        /// </summary>
        public List<OrganizationUnitPermissionsDto> OrganizationUnits { get; set; } = new();
    }
} 