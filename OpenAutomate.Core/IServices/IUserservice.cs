using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IUserService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress);
        Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = "");
        Task<UserResponse> RegisterAsync(RegistrationRequest request, string ipAddress);
        Task<UserResponse> GetByIdAsync(Guid id);

        Task<UserResponse> GetByEmailAsync(string email);

        /// <summary>
        /// Verifies a user's email using a verification token
        /// </summary>
        /// <param name="userId">The ID of the user to verify</param>
        /// <returns>True if verification succeeded, false otherwise</returns>
        Task<bool> VerifyUserEmailAsync(Guid userId);

        /// <summary>
        /// Sends a verification email to a user
        /// </summary>
        /// <param name="userId">The ID of the user to send verification to</param>
        /// <returns>True if the email was sent successfully</returns>
        Task<bool> SendVerificationEmailAsync(Guid userId);

        /// <summary>
        /// Maps a User entity to a UserResponse DTO without making a database call
        /// </summary>
        /// <param name="user">The User entity to map</param>
        /// <returns>A UserResponse DTO with the user's information</returns>
        UserResponse MapToResponse(User user);

        /// <summary>
        /// Updates the user's first name and last name
        /// </summary>
        /// <param name="userId">The ID of the user to update</param>
        /// <param name="request">The update request containing new first name and last name</param>
        /// <returns>The updated user information</returns>
        /// <exception cref="ServiceException">Thrown when user is not found or update fails</exception>
        Task<UserResponse> UpdateUserInfoAsync(Guid userId, UpdateUserInfoRequest request);

        /// <summary>
        /// Changes the user's password after verifying the current password
        /// </summary>
        /// <param name="userId">The ID of the user changing their password</param>
        /// <param name="request">The password change request containing current password and new password</param>
        /// <returns>True if password was changed successfully</returns>
        /// <exception cref="ServiceException">Thrown when user is not found, current password is incorrect, or change fails</exception>
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        /// <summary>
        /// Initiates the forgot password process for a user
        /// </summary>
        /// <param name="email">The email of the user requesting password reset</param>
        /// <returns>True if the reset email was sent successfully, false otherwise</returns>
        Task<bool> ForgotPasswordAsync(string email);

        /// <summary>
        /// Resets a user's password using a valid token
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="token">The reset token from the email</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>True if password was reset successfully, false otherwise</returns>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

        /// <summary>
        /// Gets the complete user profile with all permissions across organization units
        /// </summary>
        /// <param name="userId">The ID of the user to get profile for</param>
        /// <returns>Complete user profile with permissions for all organization units</returns>
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
    }
}
