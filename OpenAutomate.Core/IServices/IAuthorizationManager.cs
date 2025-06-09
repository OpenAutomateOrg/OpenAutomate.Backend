using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Authority;

namespace OpenAutomate.Core.IServices
{
    public interface IAuthorizationManager
    {
        // Permission checking
        Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission);
        Task<bool> HasAuthorityAsync(Guid userId, string authorityName);
        
        // Authority management
        Task<AuthorityWithPermissionsDto> CreateAuthorityAsync(CreateAuthorityDto dto);
        Task<AuthorityWithPermissionsDto?> GetAuthorityWithPermissionsAsync(Guid authorityId);
        Task<IEnumerable<AuthorityWithPermissionsDto>> GetAllAuthoritiesWithPermissionsAsync();
        Task UpdateAuthorityAsync(Guid authorityId, UpdateAuthorityDto dto);
        Task DeleteAuthorityAsync(Guid authorityId);
        
        // User-Authority assignments
        Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId);
        Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId);
        Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId);
        
        // Legacy methods (for backward compatibility)
        Task AssignAuthorityToUserAsync(Guid userId, string authorityName);
        Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName);
        
        // Resource permissions
        Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission);
        Task RemoveResourcePermissionAsync(string authorityName, string resourceName);
        
        // Resource information
        Task<IEnumerable<AvailableResourceDto>> GetAvailableResourcesAsync();

        //Assigns multiple roles to a user
        Task AssignAuthoritiesToUserAsync(Guid userId, List<Guid> authorityIds);
    }
} 