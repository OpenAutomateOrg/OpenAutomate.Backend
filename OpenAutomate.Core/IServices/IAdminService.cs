using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IAdminService
    {
        Task<UserResponse> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Updates the first and last name of a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="request">The new first and last name information.</param>
        /// <returns>The updated user information.</returns>
        Task<UserResponse> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request);

        /// <summary>
        /// Changes the password of a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>True if the password was changed successfully.</returns>
        Task<bool> ChangePasswordAsync(Guid userId, string newPassword);

        /// <summary>
        /// Maps a User entity to a UserResponse DTO without making a database call
        /// </summary>
        /// <param name="user">The User entity to map</param>
        /// <returns>A UserResponse DTO with the user's information</returns>
        UserResponse MapToResponse(User user);
    }
}
