using OpenAutomate.Core.Dto.UserDto;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for authentication and identity operations
    /// </summary>
    /// <remarks>
    /// This interface handles all identity-related operations including:
    /// - User registration
    /// - Authentication (login/logout)
    /// - Token management (refresh, revoke)
    /// - Password recovery
    /// </remarks>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <param name="request">The registration information</param>
        /// <param name="ipAddress">The client IP address for tracking</param>
        /// <returns>User response with initial authentication data</returns>
        Task<UserResponse> RegisterAsync(RegistrationRequest request, string ipAddress);

        /// <summary>
        /// Authenticates a user and provides access tokens
        /// </summary>
        /// <param name="request">The authentication request</param>
        /// <param name="ipAddress">The client IP address for tracking</param>
        /// <returns>Authentication response with tokens</returns>
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress);

        /// <summary>
        /// Generates a new access token using a valid refresh token
        /// </summary>
        /// <param name="refreshToken">The current refresh token</param>
        /// <param name="ipAddress">The client IP address for tracking</param>
        /// <returns>New authentication response with updated tokens</returns>
        Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken, string ipAddress);

        /// <summary>
        /// Revokes an active refresh token
        /// </summary>
        /// <param name="token">The token to revoke</param>
        /// <param name="ipAddress">The client IP address for tracking</param>
        /// <param name="reason">Optional reason for revocation</param>
        /// <returns>True if the token was successfully revoked</returns>
        Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = "");

        /// <summary>
        /// Initiates password recovery process by sending reset email
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <returns>True if reset email was sent successfully</returns>
        Task<bool> ForgotPasswordAsync(string email);

        /// <summary>
        /// Resets user password using a valid reset token
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="token">The password reset token</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>True if password was reset successfully</returns>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

        /// <summary>
        /// Sends email verification to a user
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>True if verification email was sent successfully</returns>
        Task<bool> SendVerificationEmailAsync(Guid userId);

        /// <summary>
        /// Maps a user entity to a response DTO
        /// </summary>
        /// <param name="user">The user entity</param>
        /// <returns>User response DTO</returns>
        UserResponse MapToResponse(Domain.Entities.User user);
    }
} 