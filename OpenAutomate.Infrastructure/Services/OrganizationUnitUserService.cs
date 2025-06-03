using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
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

            var result = from ouu in orgUnitUsers
                         join u in users on ouu.UserId equals u.Id
                         join ua in userAuthorities on ouu.UserId equals ua.UserId into uaGroup
                         from ua in uaGroup.DefaultIfEmpty()
                         join a in authorities on ua != null ? ua.AuthorityId : Guid.Empty equals a.Id into aGroup
                         from a in aGroup.DefaultIfEmpty()
                         select new OrganizationUnitUserDetailDto
                         {
                             UserId = u.Id,
                             Email = u.Email ?? string.Empty,
                             FirstName = u.FirstName ?? string.Empty,
                             LastName = u.LastName ?? string.Empty,
                             Role = a != null ? a.Name : string.Empty,
                             JoinedAt = ouu.CreatedAt
                         };
            var rolePriority = new List<string> { "OWNER", "OPERATOR", "DEVELOPER", "USER" };
            var grouped = result
                .GroupBy(x => x.UserId)
                .Select(g => g.OrderBy(x => rolePriority.IndexOf(x.Role)).First());
            return grouped.ToList();
        }
    }
}