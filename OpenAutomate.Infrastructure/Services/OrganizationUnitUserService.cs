using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.Dto.OrganizationUnitUser;
using OpenAutomate.Core.Dto.Common;
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

        public async Task<BulkDeleteResultDto> BulkRemoveUsersAsync(string tenantSlug, List<Guid> userIds, Guid currentUserId)
        {
            var result = new BulkDeleteResultDto
            {
                TotalRequested = userIds.Count
            };
            var successfullyProcessedIds = new List<Guid>();

            try
            {
                // Get organization unit
                var ou = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == tenantSlug);
                if (ou == null)
                {
                    HandleOrganizationUnitNotFound(userIds, tenantSlug, result);
                    return result;
                }

                // Process each user
                foreach (var userId in userIds)
                {
                    await RemoveSingleUserAsync(userId, currentUserId, ou, tenantSlug, successfullyProcessedIds, result);
                }

                // Commit all changes atomically at the end
                if (successfullyProcessedIds.Count > 0)
                {
                    await _unitOfWork.CompleteAsync();

                    // Only now update result with successful deletions
                    result.DeletedIds.AddRange(successfullyProcessedIds);
                    result.SuccessfullyDeleted = successfullyProcessedIds.Count;
                }

                return result;
            }
            catch (Exception ex)
            {
                HandleOperationFailure(userIds, result, ex);
                return result;
            }
        }

        /// <summary>
        /// Handles the case when organization unit is not found
        /// </summary>
        private void HandleOrganizationUnitNotFound(List<Guid> userIds, string tenantSlug, BulkDeleteResultDto result)
        {
            foreach (var userId in userIds)
            {
                result.Errors.Add(new BulkDeleteErrorDto
                {
                    Id = userId,
                    ErrorMessage = $"Organization unit '{tenantSlug}' not found",
                    ErrorCode = "OrganizationUnitNotFound"
                });
            }
            result.Failed = result.Errors.Count;
        }

        /// <summary>
        /// Removes a single user from the organization unit with validation and error handling
        /// </summary>
        private async Task RemoveSingleUserAsync(Guid userId, Guid currentUserId, Core.Domain.Entities.OrganizationUnit ou,
            string tenantSlug, List<Guid> successfullyProcessedIds, BulkDeleteResultDto result)
        {
            try
            {
                // Skip self-removal
                if (userId == currentUserId)
                {
                    result.Errors.Add(new BulkDeleteErrorDto
                    {
                        Id = userId,
                        ErrorMessage = "Cannot remove yourself from the organization unit",
                        ErrorCode = "SelfRemoval"
                    });
                    result.Failed++;
                    return;
                }

                // Check if user exists in OU
                var orgUnitUser = (await _unitOfWork.OrganizationUnitUsers
                    .GetAllAsync(ouu => ouu.OrganizationUnitId == ou.Id && ouu.UserId == userId)).FirstOrDefault();

                if (orgUnitUser == null)
                {
                    result.Errors.Add(new BulkDeleteErrorDto
                    {
                        Id = userId,
                        ErrorMessage = $"User not found in organization unit '{tenantSlug}'",
                        ErrorCode = "UserNotFoundInOU"
                    });
                    result.Failed++;
                    return;
                }

                // Remove user authorities in this OU
                var userAuthorities = await _unitOfWork.UserAuthorities
                    .GetAllAsync(ua => ua.OrganizationUnitId == ou.Id && ua.UserId == userId);
                _unitOfWork.UserAuthorities.RemoveRange(userAuthorities);

                // Remove OU user relationship
                _unitOfWork.OrganizationUnitUsers.Remove(orgUnitUser);

                // Track for commit - only update final result after successful commit
                successfullyProcessedIds.Add(userId);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkDeleteErrorDto
                {
                    Id = userId,
                    ErrorMessage = $"Error removing user: {ex.Message}",
                    ErrorCode = "RemovalError"
                });
                result.Failed++;
            }
        }

        /// <summary>
        /// Handles general operation failures by marking remaining users as failed
        /// </summary>
        private void HandleOperationFailure(List<Guid> userIds, BulkDeleteResultDto result, Exception ex)
        {
            foreach (var userId in userIds.Except(result.DeletedIds))
            {
                if (!result.Errors.Any(e => e.Id == userId))
                {
                    result.Errors.Add(new BulkDeleteErrorDto
                    {
                        Id = userId,
                        ErrorMessage = $"Operation failed: {ex.Message}",
                        ErrorCode = "OperationError"
                    });
                }
            }
            result.Failed = result.Errors.Count;
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