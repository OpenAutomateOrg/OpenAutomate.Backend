using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.OrganizationUnit;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationUnitService
    {
        /// <summary>
        /// Creates a new organization unit with default authorities and assigns the creator as OWNER
        /// </summary>
        Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId);
        
        /// <summary>
        /// Gets an organization unit by its ID
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid id);
        
        /// <summary>
        /// Gets an organization unit by its slug
        /// </summary>
        Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug);
        
        /// <summary>
        /// Gets all organization units
        /// </summary>
        Task<IEnumerable<OrganizationUnitResponseDto>> GetAllOrganizationUnitsAsync();
        
        /// <summary>
        /// Updates an organization unit
        /// </summary>
        Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid id, CreateOrganizationUnitDto dto);
        
        /// <summary>
        /// Checks the impact of changing an organization unit's name
        /// </summary>
        Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid id, string newName);
        
        /// <summary>
        /// Generates a slug from the organization unit name
        /// </summary>
        string GenerateSlugFromName(string name);
        
        /// <summary>
        /// Gets all organization units that a user belongs to, regardless of role
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A response containing organization units and the total count</returns>
        Task<UserOrganizationUnitsResponseDto> GetUserOrganizationUnitsAsync(Guid userId);
    }
} 