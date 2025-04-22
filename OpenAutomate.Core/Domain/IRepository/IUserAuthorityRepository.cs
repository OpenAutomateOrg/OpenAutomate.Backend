using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.IRepository
{
    public interface IUserAuthorityRepository : IRepository<UserAuthority>
    {
        Task<IEnumerable<UserAuthority>> GetByUserIdAsync(Guid userId);
        Task<bool> HasAuthorityAsync(Guid userId, string authorityName);
        Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId);
        Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId);
    }
} 