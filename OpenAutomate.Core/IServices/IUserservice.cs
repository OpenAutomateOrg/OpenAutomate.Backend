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
        Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = null);
        Task<UserResponse> RegisterAsync(RegistrationRequest request, string ipAddress);
        Task<UserResponse> GetByIdAsync(Guid id);

        Task<UserResponse> GetByEmailAsync(string email);
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();
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
    }
}
