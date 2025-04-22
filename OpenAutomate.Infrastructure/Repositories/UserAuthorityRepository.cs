using Microsoft.EntityFrameworkCore;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Repositories
{
    public class UserAuthorityRepository : Repository<UserAuthority>, IUserAuthorityRepository
    {
        private readonly ApplicationDbContext _context;
        
        public UserAuthorityRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<UserAuthority>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Set<UserAuthority>()
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Authority)
                .ToListAsync();
        }
        
        public async Task<bool> HasAuthorityAsync(Guid userId, string authorityName)
        {
            var authority = await _context.Set<Authority>()
                .FirstOrDefaultAsync(a => a.Name == authorityName);
                
            if (authority == null) 
                return false;
            
            return await _context.Set<UserAuthority>()
                .AnyAsync(ua => ua.UserId == userId && ua.AuthorityId == authority.Id);
        }
        
        public async Task AssignAuthorityToUserAsync(Guid userId, Guid authorityId)
        {
            // Check if assignment already exists
            var exists = await _context.Set<UserAuthority>()
                .AnyAsync(ua => ua.UserId == userId && ua.AuthorityId == authorityId);
                
            if (!exists)
            {
                // Get the user's organization unit ID
                Guid organizationUnitId;
                
                try
                {
                    // Try to get the organization unit from the user's relationship
                    var userOrgUnit = await _context.Set<OrganizationUnitUser>()
                        .FirstOrDefaultAsync(ouu => ouu.UserId == userId);
                        
                    if (userOrgUnit == null)
                        throw new InvalidOperationException("User does not belong to any organization unit");
                        
                    organizationUnitId = userOrgUnit.OrganizationUnitId;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to determine organization unit for user: {ex.Message}");
                }
                
                var userAuthority = new UserAuthority
                {
                    UserId = userId,
                    AuthorityId = authorityId,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _context.Set<UserAuthority>().AddAsync(userAuthority);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task RemoveAuthorityFromUserAsync(Guid userId, Guid authorityId)
        {
            var userAuthority = await _context.Set<UserAuthority>()
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AuthorityId == authorityId);
                
            if (userAuthority != null)
            {
                _context.Set<UserAuthority>().Remove(userAuthority);
                await _context.SaveChangesAsync();
            }
        }
    }
} 