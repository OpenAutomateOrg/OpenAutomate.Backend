using OpenAutomate.Core.Dto.UserDto;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for user account self-service operations
    /// </summary>
    /// <remarks>
    /// This interface handles all operations a user can perform on their own account:
    /// - Getting their profile information
    /// - Updating their personal information
    /// - Changing their password
    /// </remarks>
    public interface IAccountService
    {
        /// <summary>
        /// Gets the complete user profile with permissions across all organization units
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>Complete user profile including permissions</returns>
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);

        /// <summary>
        /// Updates the user's personal information (first name, last name)
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="request">The updated user information</param>
        /// <returns>Updated user response</returns>
        Task<UserResponse> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request);

        /// <summary>
        /// Changes the user's password
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="request">The password change request</param>
        /// <returns>True if password was changed successfully</returns>
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
} 