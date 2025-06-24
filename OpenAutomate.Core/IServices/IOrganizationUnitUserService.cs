using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.Dto.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IOrganizationUnitUserService
    {
        /// <summary>
        /// Gets all users in a specific organization unit by tenant slug
        /// </summary>
        /// <param name="tenantSlug">The slug of the organization unit (tenant)</param>
        /// <returns>List of users in the organization unit</returns>
        Task<IEnumerable<OrganizationUnitUserDetailDto>> GetUsersInOrganizationUnitAsync(string tenantSlug);
        Task<bool> DeleteUserAsync(string tenantSlug, Guid userId);
        Task<IEnumerable<AuthorityDto>> GetRolesInOrganizationUnitAsync(string tenantSlug);
    }
}
