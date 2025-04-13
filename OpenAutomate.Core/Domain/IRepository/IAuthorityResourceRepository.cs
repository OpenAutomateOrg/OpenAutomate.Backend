using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.IRepository
{
    public interface IAuthorityResourceRepository : IRepository<AuthorityResource>
    {
        Task<IEnumerable<AuthorityResource>> GetByAuthorityIdAsync(Guid authorityId);
        Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission);
    }
} 