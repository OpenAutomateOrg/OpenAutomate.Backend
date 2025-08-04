using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork unitOfWork, ILogger<AdminService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return users.Select(u => MapToResponse(u));
        }

        public async Task<UserResponse> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                return MapToResponse(user);
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Error retrieving user with ID: {userId}", ex);
            }
        }

        public async Task<UserResponse> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) throw new ServiceException($"User with ID {userId} not found");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Admin updated user info for user: {UserId}", userId);

            return MapToResponse(user);
        }
        public async Task<bool> ChangePasswordAsync(Guid userId, string newPassword)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) throw new ServiceException($"User with ID {userId} not found");

            CreatePasswordHash(newPassword, out string hash, out string salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }

        public UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsEmailVerified = user.IsEmailVerified,
                SystemRole = user.SystemRole,
                CreatedAt = user.CreatedAt
            };
        }

        public OrganizationUnitResponseDto MapToOrganizationUnitResponseDto(OrganizationUnit organizationUnit)
        {
            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                UpdatedAt = organizationUnit.LastModifyAt
            };
        }

        #region Private Helper Methods

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            byte[] saltBytes = hmac.Key;
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Convert to Base64 strings for storage
            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }

        #endregion

        public async Task<IEnumerable<OrganizationUnitResponseDto>> GetAllOrganizationUnitsAsync()
        {
            var organizationUnits = await _unitOfWork.OrganizationUnits.GetAllAsync();
            return organizationUnits.Select(MapToOrganizationUnitResponseDto);
        }
    }
}
