using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;

namespace OpenAutomate.Infrastructure.Repositories
{
    public class AuthorityRepository : Repository<Authority>, IAuthorityRepository
    {
        private readonly ApplicationDbContext _context;
        
        public AuthorityRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<Authority> GetByNameAsync(string name)
        {
            return await _context.Set<Authority>()
                .FirstOrDefaultAsync(a => a.Name == name);
        }
        
        public async Task<IEnumerable<Authority>> GetUserAuthoritiesAsync(Guid userId)
        {
            return await _context.Set<UserAuthority>()
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.Authority)
                .ToListAsync();
        }
    }
} 