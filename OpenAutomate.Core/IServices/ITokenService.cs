using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for JWT token generation and refresh token management
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates access and refresh tokens for a user
        /// </summary>
        /// <param name="user">The user to generate tokens for</param>
        /// <param name="ipAddress">IP address of the client</param>
        /// <returns>Authentication response with tokens</returns>
        AuthenticationResponse GenerateTokens(User user, string ipAddress);

        /// <summary>
        /// Refreshes an access token using a refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <param name="ipAddress">IP address of the client</param>
        /// <returns>Authentication response with new tokens</returns>
        Task<AuthenticationResponse> RefreshToken(string refreshToken, string ipAddress);

        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        /// <param name="token">The refresh token to revoke</param>
        /// <param name="ipAddress">IP address of the client</param>
        /// <param name="reason">Reason for revocation</param>
        /// <returns>True if successful, false otherwise</returns>
        bool RevokeToken(string token, string ipAddress, string reason = null);

        /// <summary>
        /// Validates an access token
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateToken(string token);
    }
}