using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;

namespace OpenAutomate.Infrastructure.Repositories
{
    public class AuthorityResourceRepository : Repository<AuthorityResource>, IAuthorityResourceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserAuthorityRepository _userAuthorityRepository;
        
        public AuthorityResourceRepository(
            ApplicationDbContext context,
            IUserAuthorityRepository userAuthorityRepository) : base(context)
        {
            _context = context;
            _userAuthorityRepository = userAuthorityRepository;
        }
        
        public async Task<IEnumerable<AuthorityResource>> GetByAuthorityIdAsync(Guid authorityId)
        {
            return await _context.Set<AuthorityResource>()
                .Where(ar => ar.AuthorityId == authorityId)
                .ToListAsync();
        }
        
        public async Task<bool> HasPermissionAsync(Guid userId, string resourceName, int permission)
        {
            // Get user's authorities
            var userAuthorities = await _userAuthorityRepository.GetByUserIdAsync(userId);
            if (userAuthorities == null || !userAuthorities.Any())
                return false;
                
            var authorityIds = userAuthorities.Select(ua => ua.AuthorityId);
            
            // Check if any of user's authorities has the required permission for the resource
            return await _context.Set<AuthorityResource>()
                .AnyAsync(ar => 
                    authorityIds.Contains(ar.AuthorityId) && 
                    ar.ResourceName == resourceName && 
                    ar.Permission >= permission);
        }
    }
} 