using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.Authority;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for user account self-service operations
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IUnitOfWork unitOfWork,
            ILogger<AccountService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
        {
            try
            {
                // Get user with basic information
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    throw new ServiceException($"User with ID {userId} not found");

                // Get all user authorities across all organization units (ignoring tenant filters)
                var userAuthorities = await _unitOfWork.UserAuthorities.GetAllIgnoringFiltersAsync(
                    ua => ua.UserId == userId);

                // Get all organization units the user belongs to in a single batch query
                var organizationUnitIds = userAuthorities.Select(ua => ua.OrganizationUnitId).Distinct().ToList();
                var organizationUnits = await _unitOfWork.OrganizationUnits.GetAllIgnoringFiltersAsync(
                    ou => organizationUnitIds.Contains(ou.Id));

                // Get all authority resources for all user authorities in a single batch query
                var allAuthorityIds = userAuthorities.Select(ua => ua.AuthorityId).Distinct().ToList();
                var allAuthorityResources = await _unitOfWork.AuthorityResources.GetAllIgnoringFiltersAsync(
                    ar => allAuthorityIds.Contains(ar.AuthorityId));

                // Build the profile DTO
                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    SystemRole = user.SystemRole,
                    OrganizationUnits = new List<OrganizationUnitPermissionsDto>()
                };

                // Process each organization unit
                foreach (var orgUnit in organizationUnits)
                {
                    var orgUnitPermissions = new OrganizationUnitPermissionsDto
                    {
                        Id = orgUnit.Id,
                        Name = orgUnit.Name,
                        Slug = orgUnit.Slug,
                        Permissions = new List<ResourcePermissionDto>()
                    };

                    // Get user's authorities in this organization unit
                    var userAuthoritiesInOrgUnit = userAuthorities.Where(ua => ua.OrganizationUnitId == orgUnit.Id).ToList();
                    var authorityIds = userAuthoritiesInOrgUnit.Select(ua => ua.AuthorityId).ToList();

                    // Filter authority resources for this organization unit from the pre-fetched data
                    var authorityResources = allAuthorityResources.Where(ar => authorityIds.Contains(ar.AuthorityId));

                    // Calculate the highest permission level for each resource
                    var resourcePermissions = new Dictionary<string, int>();

                    foreach (var resource in authorityResources)
                    {
                        var resourceName = resource.ResourceName;
                        var permission = resource.Permission;

                        // Keep the highest permission level for each resource
                        if (!resourcePermissions.ContainsKey(resourceName) || 
                            resourcePermissions[resourceName] < permission)
                        {
                            resourcePermissions[resourceName] = permission;
                        }
                    }

                    // Convert to DTOs
                    orgUnitPermissions.Permissions = resourcePermissions
                        .Select(rp => new ResourcePermissionDto
                        {
                            ResourceName = rp.Key,
                            Permission = rp.Value
                        })
                        .OrderBy(rp => rp.ResourceName)
                        .ToList();

                    profile.OrganizationUnits.Add(orgUnitPermissions);
                }

                // Sort organization units by name for consistency
                profile.OrganizationUnits = profile.OrganizationUnits
                    .OrderBy(ou => ou.Name)
                    .ToList();

                _logger.LogInformation("User profile retrieved for user: {UserId} with {OrgUnitsCount} organization units", 
                    userId, profile.OrganizationUnits.Count);

                return profile;
            }
            catch (ServiceException)
            {
                // Rethrow service exceptions as they are already properly typed
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for user: {UserId}", userId);
                throw new ServiceException($"Error retrieving user profile for user: {userId}", ex);
            }
        }

        public async Task<UserResponse> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new ServiceException($"User with ID {userId} not found");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("User info updated for user: {UserId}", userId);
            return MapToResponse(user);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new ServiceException($"User with ID {userId} not found");

            // Verify current password
            if (!VerifyPasswordHash(request.CurrentPassword, user.PasswordHash ?? string.Empty, user.PasswordSalt ?? string.Empty))
                throw new ServiceException("Current password is incorrect");

            // Set new password
            CreatePasswordHash(request.NewPassword, out string newHash, out string newSalt);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }

        #region Private Helper Methods

        private static UserResponse MapToResponse(User user)
        {               
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsEmailVerified = user.IsEmailVerified,
                SystemRole = user.SystemRole
            };
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            byte[] saltBytes = hmac.Key;
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            // Convert to Base64 strings for storage
            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            // Convert from Base64 strings back to bytes
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);
            
            using var hmac = new HMACSHA512(saltBytes);
            byte[] computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            // Compare the computed hash with the stored hash
            if (storedHashBytes.Length != computedHashBytes.Length)
                return false;
                
            for (int i = 0; i < computedHashBytes.Length; i++)
            {
                if (computedHashBytes[i] != storedHashBytes[i])
                    return false;
            }
            
            return true;
        }

        #endregion
    }
} 