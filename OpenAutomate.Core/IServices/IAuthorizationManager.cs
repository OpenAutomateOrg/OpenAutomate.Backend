using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.IServices
{
    public interface IAuthorizationManager
    {
        Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission);
        Task<bool> HasAuthorityAsync(Guid userId, string authorityName);
        Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId);
        Task AssignAuthorityToUserAsync(Guid userId, string authorityName);
        Task RemoveAuthorityFromUserAsync(Guid userId, string authorityName);
        Task AddResourcePermissionAsync(string authorityName, string resourceName, int permission);
        Task RemoveResourcePermissionAsync(string authorityName, string resourceName);
    }
} 