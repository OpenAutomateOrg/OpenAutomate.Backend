using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.OrganizationUnit;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationUnitService
    {
        /// <summary>
        /// Creates a new organization unit with the specified user as the owner
        /// </summary>
        Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId);
        
        /// <summary>
        /// Gets an organization unit by its ID
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid organizationUnitId);
        
        /// <summary>
        /// Gets an organization unit by its slug
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug);
        
        /// <summary>
        /// Gets all organization units the user belongs to
        /// </summary>
        Task<IEnumerable<OrganizationUnitResponseDto>> GetUserOrganizationUnitsAsync(Guid userId);
        
        /// <summary>
        /// Checks the impact of changing an organization unit's name
        /// </summary>
        Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid organizationUnitId, string newName);
        
        /// <summary>
        /// Updates an organization unit's information
        /// </summary>
        Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid organizationUnitId, UpdateOrganizationUnitDto dto);
        
        /// <summary>
        /// Deactivates (soft deletes) an organization unit
        /// </summary>
        Task<bool> DeactivateOrganizationUnitAsync(Guid organizationUnitId);
        
        /// <summary>
        /// Adds a user to an organization unit with the specified role
        /// </summary>
        Task<bool> AddUserToOrganizationUnitAsync(Guid organizationUnitId, Guid userId, string role = "Member");
        
        /// <summary>
        /// Removes a user from an organization unit
        /// </summary>
        Task<bool> RemoveUserFromOrganizationUnitAsync(Guid organizationUnitId, Guid userId);
        
        /// <summary>
        /// Gets all users belonging to an organization unit
        /// </summary>
        Task<IEnumerable<OrganizationUnitUserDto>> GetOrganizationUnitUsersAsync(Guid organizationUnitId);
        
        /// <summary>
        /// Checks if a user has access to an organization unit
        /// </summary>
        Task<bool> UserHasAccessToOrganizationUnitAsync(Guid organizationUnitId, Guid userId);
    }
} 