using OpenAutomate.Core.Dto.Authority;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    /// <summary>
    /// Organization unit with user's permissions within it
    /// </summary>
    public class OrganizationUnitPermissionsDto
    {
        /// <summary>
        /// Organization unit's unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization unit name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Organization unit URL-friendly slug
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// User's permissions for each resource within this organization unit
        /// </summary>
        public List<ResourcePermissionDto> Permissions { get; set; } = new();
    }
} 