using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Redis-based implementation of JWT token blocklist service
/// </summary>
public class JwtBlocklistService : IJwtBlocklistService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<JwtBlocklistService> _logger;

    // Log message templates
    private static class LogMessages
    {
        public const string TokenBlocklisted = "JWT token {JwtTokenId} blocklisted until {ExpiresAt}";
        public const string TokenCheckBlocklisted = "JWT token {JwtTokenId} is blocklisted";
        public const string TokenCheckNotBlocklisted = "JWT token {JwtTokenId} is not blocklisted";
        public const string TokenRemovedFromBlocklist = "JWT token {JwtTokenId} removed from blocklist";
        public const string AllUserTokensBlocked = "All tokens blocked for user {UserId} - {BlockedCount} entries added";
        public const string BlocklistOperationFailed = "Blocklist operation failed for token {JwtTokenId}";
    }

    public JwtBlocklistService(
        ICacheService cacheService,
        ILogger<JwtBlocklistService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> BlocklistTokenAsync(string jwtTokenId, DateTime expiresAt, string reason = "")
    {
        try
        {
            if (string.IsNullOrEmpty(jwtTokenId))
            {
                return false;
            }

            var cacheKey = GetBlocklistCacheKey(jwtTokenId);
            var ttl = expiresAt - DateTime.UtcNow;

            // Only cache if the token hasn't already expired
            if (ttl > TimeSpan.Zero)
            {
                var blocklistEntry = new BlocklistEntry
                {
                    TokenId = jwtTokenId,
                    BlockedAt = DateTime.UtcNow,
                    Reason = reason,
                    ExpiresAt = expiresAt
                };

                var success = await _cacheService.SetAsync(cacheKey, blocklistEntry, ttl);
                
                if (success)
                {
                    _logger.LogInformation(LogMessages.TokenBlocklisted, jwtTokenId, expiresAt);
                }
                
                return success;
            }

            return true; // Token already expired, no need to blocklist
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.BlocklistOperationFailed, jwtTokenId);
            return false;
        }
    }

    public async Task<bool> IsTokenBlocklistedAsync(string jwtTokenId)
    {
        try
        {
            if (string.IsNullOrEmpty(jwtTokenId))
            {
                return false;
            }

            var cacheKey = GetBlocklistCacheKey(jwtTokenId);
            var blocklistEntry = await _cacheService.GetAsync<BlocklistEntry>(cacheKey);

            var isBlocklisted = blocklistEntry != null;
            
            if (isBlocklisted)
            {
                _logger.LogDebug(LogMessages.TokenCheckBlocklisted, jwtTokenId);
            }
            else
            {
                _logger.LogDebug(LogMessages.TokenCheckNotBlocklisted, jwtTokenId);
            }

            return isBlocklisted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.BlocklistOperationFailed, jwtTokenId);
            // In case of error, err on the side of caution and don't block valid tokens
            return false;
        }
    }

    public async Task<bool> RemoveFromBlocklistAsync(string jwtTokenId)
    {
        try
        {
            if (string.IsNullOrEmpty(jwtTokenId))
            {
                return false;
            }

            var cacheKey = GetBlocklistCacheKey(jwtTokenId);
            var success = await _cacheService.RemoveAsync(cacheKey);
            
            if (success)
            {
                _logger.LogInformation(LogMessages.TokenRemovedFromBlocklist, jwtTokenId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LogMessages.BlocklistOperationFailed, jwtTokenId);
            return false;
        }
    }

    public async Task<int> BlocklistAllUserTokensAsync(Guid userId, string reason = "")
    {
        try
        {
            // Note: This is a simplified implementation
            // In a production system, you would maintain a mapping of user IDs to active JWT IDs
            // or use a pattern-based approach with Redis SCAN
            
            // For now, we'll create a user-level blocklist entry that can be checked
            // This would require modifying the token validation to also check user-level blocks
            
            var userBlocklistKey = GetUserBlocklistCacheKey(userId);
            var blocklistEntry = new UserBlocklistEntry
            {
                UserId = userId,
                BlockedAt = DateTime.UtcNow,
                Reason = reason
            };

            // Set a long TTL for user-level blocks (24 hours)
            // This gives time for all tokens to naturally expire
            var ttl = TimeSpan.FromHours(24);
            var success = await _cacheService.SetAsync(userBlocklistKey, blocklistEntry, ttl);
            
            if (success)
            {
                _logger.LogInformation(LogMessages.AllUserTokensBlocked, userId, 1);
                return 1;
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to block all tokens for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Checks if all tokens for a user are blocked
    /// </summary>
    public async Task<bool> IsUserBlocklistedAsync(Guid userId)
    {
        try
        {
            var userBlocklistKey = GetUserBlocklistCacheKey(userId);
            var blocklistEntry = await _cacheService.GetAsync<UserBlocklistEntry>(userBlocklistKey);
            return blocklistEntry != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check user blocklist for user {UserId}", userId);
            return false;
        }
    }

    #region Cache Key Generation

    private static string GetBlocklistCacheKey(string jwtTokenId)
    {
        return $"jwt-blocklist:{jwtTokenId}";
    }

    private static string GetUserBlocklistCacheKey(Guid userId)
    {
        return $"jwt-user-blocklist:{userId}";
    }

    #endregion

    #region Cache DTOs

    private class BlocklistEntry
    {
        public string TokenId { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class UserBlocklistEntry
    {
        public Guid UserId { get; set; }
        public DateTime BlockedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    #endregion
} 