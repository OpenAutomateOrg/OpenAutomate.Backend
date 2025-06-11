using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationUnitUserService : IOrganizationUnitUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrganizationUnitUserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<OrganizationUnitUserDetailDto>> GetUsersInOrganizationUnitAsync(string tenantSlug)
        {
            var ou = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == tenantSlug);
            if (ou == null) return new List<OrganizationUnitUserDetailDto>();
            var orgUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync(ouu => ouu.OrganizationUnitId == ou.Id);
            var userIds = orgUnitUsers.Select(ouu => ouu.UserId).ToList();
            if (userIds.Count == 0) return new List<OrganizationUnitUserDetailDto>();
            var users = await _unitOfWork.Users.GetAllAsync(u => userIds.Contains(u.Id));
            var userAuthorities = await _unitOfWork.UserAuthorities.GetAllAsync(ua => ua.OrganizationUnitId == ou.Id);
            var authorities = await _unitOfWork.Authorities.GetAllAsync(a => a.OrganizationUnitId == ou.Id);

            // Build a lookup for userId -> list of role names
            var userRolesLookup = userAuthorities
                .Join(authorities, ua => ua.AuthorityId, a => a.Id, (ua, a) => new { ua.UserId, RoleName = a.Name })
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var result = from ouu in orgUnitUsers
                         join u in users on ouu.UserId equals u.Id
                         select new OrganizationUnitUserDetailDto
                         {
                             UserId = u.Id,
                             Email = u.Email ?? string.Empty,
                             FirstName = u.FirstName ?? string.Empty,
                             LastName = u.LastName ?? string.Empty,
                             Roles = userRolesLookup.TryGetValue(u.Id, out var roles) ? roles : new List<string>(),
                             JoinedAt = ouu.CreatedAt
                         };

            // Ensure no duplicate users
            return result.GroupBy(x => x.UserId).Select(g => g.First()).ToList();
        }

        public async Task<bool> DeleteUserAsync(string tenantSlug, Guid userId)
        {
            var ou = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == tenantSlug);
            if (ou == null)
                return false;

            var orgUnitUser = (await _unitOfWork.OrganizationUnitUsers
                .GetAllAsync(ouu => ouu.OrganizationUnitId == ou.Id && ouu.UserId == userId)).FirstOrDefault();
            if (orgUnitUser == null)
                return false;
            var userAuthorities = await _unitOfWork.UserAuthorities
                .GetAllAsync(ua => ua.OrganizationUnitId == ou.Id && ua.UserId == userId);
            _unitOfWork.UserAuthorities.RemoveRange(userAuthorities);
            _unitOfWork.OrganizationUnitUsers.Remove(orgUnitUser);

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<AuthorityDto>> GetRolesInOrganizationUnitAsync(string tenantSlug)
        {
            var ou = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == tenantSlug);
            if (ou == null) return new List<AuthorityDto>();
            var authorities = await _unitOfWork.Authorities.GetAllAsync(a => a.OrganizationUnitId == ou.Id);
            return authorities.Select(a => new AuthorityDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description
            }).ToList();
        }
    }
}