namespace OpenAutomate.Core.IServices;

/// <summary>
/// Service for managing JWT token blocklist to enable immediate token revocation
/// </summary>
public interface IJwtBlocklistService
{
    /// <summary>
    /// Adds a JWT token to the blocklist
    /// </summary>
    /// <param name="jwtTokenId">The JTI (JWT ID) of the token to blocklist</param>
    /// <param name="expiresAt">When the token naturally expires (for TTL)</param>
    /// <param name="reason">Reason for blocklisting</param>
    /// <returns>True if successfully added to blocklist</returns>
    Task<bool> BlocklistTokenAsync(string jwtTokenId, DateTime expiresAt, string reason = "");

    /// <summary>
    /// Checks if a JWT token is blocklisted
    /// </summary>
    /// <param name="jwtTokenId">The JTI (JWT ID) to check</param>
    /// <returns>True if the token is blocklisted</returns>
    Task<bool> IsTokenBlocklistedAsync(string jwtTokenId);

    /// <summary>
    /// Removes a token from the blocklist (if needed for testing or admin purposes)
    /// </summary>
    /// <param name="jwtTokenId">The JTI (JWT ID) to remove from blocklist</param>
    /// <returns>True if successfully removed</returns>
    Task<bool> RemoveFromBlocklistAsync(string jwtTokenId);

    /// <summary>
    /// Blocks all tokens for a specific user (useful for security incidents)
    /// </summary>
    /// <param name="userId">The user ID whose tokens should be blocked</param>
    /// <param name="reason">Reason for blocking all user tokens</param>
    /// <returns>Number of tokens blocked</returns>
    Task<int> BlocklistAllUserTokensAsync(Guid userId, string reason = "");

    /// <summary>
    /// Checks if all tokens for a user are blocked
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if all user tokens are blocked</returns>
    Task<bool> IsUserBlocklistedAsync(Guid userId);
} 