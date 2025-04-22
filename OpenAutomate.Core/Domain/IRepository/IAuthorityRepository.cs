using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.Domain.IRepository
{
    public interface IAuthorityRepository : IRepository<Authority>
    {
        Task<Authority> GetByNameAsync(string name);
        Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId);
    }
} 