using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Exceptions;

namespace OpenAutomate.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext,
            ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public AuthenticationResponse GenerateTokens(User user, string ipAddress)
        {
            try
            {
                _logger.LogInformation("Generating tokens for user {UserId} - {UserEmail}", user.Id, user.Email);
                
                // Generate JWT token
                var token = GenerateJwtToken(user);

                // Generate refresh token
                var refreshToken = GenerateRefreshToken(ipAddress);
                
                // Add refresh token to user
                AddRefreshTokenToUserAsync(user, refreshToken).GetAwaiter().GetResult();

                _logger.LogInformation("Generated refresh token {Token} for user {UserId}", refreshToken.Token.Substring(0, 10), user.Id);

                return new AuthenticationResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    SystemRole = user.SystemRole,
                    Token = token,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiration = refreshToken.Expires
                };
            }
            catch (OpenAutomateException)
            {
                // Rethrow custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error saving refresh token for user {UserId}", user.Id);
                throw new ServiceException($"Failed to generate tokens for user {user.Id}: {ex.Message}", ex);
            }
        }

        public AuthenticationResponse RefreshToken(string refreshToken, string ipAddress)
        {
            try
            {
                _logger.LogInformation("Processing refresh token {Token}", refreshToken.Substring(0, 10));
                
                // First find the token in the database with the exact token string
                var oldToken = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Token == refreshToken).GetAwaiter().GetResult();
                    
                if (oldToken == null)
                {
                    _logger.LogWarning("Invalid token: Token not found in database");
                    throw new TokenException("Invalid token");
                }
                    
                // Then check the computed properties in memory
                if (oldToken.IsRevoked || oldToken.IsExpired)
                {
                    _logger.LogWarning("Token is revoked or expired. Revoked: {IsRevoked}, Expired: {IsExpired}", 
                        oldToken.IsRevoked, oldToken.IsExpired);
                    throw new TokenException("Token is revoked or expired");
                }
                
                // Get the full user information directly from the database by ID
                var userId = oldToken.UserId;
                
                // Get the user by ID - critical to ensure we get the correct user
                var user = _unitOfWork.Users.GetByIdAsync(userId).GetAwaiter().GetResult();
                
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID {UserId}", userId);
                    throw new AuthenticationException("User not found");
                }
                
                // Verify the token belongs to this user
                if (!user.OwnsToken(refreshToken))
                {
                    _logger.LogWarning("Token does not belong to user {UserId}", user.Id);
                    throw new TokenException("Invalid token for user");
                }
                
                // Mark the old token as revoked
                oldToken.Revoked = DateTime.UtcNow;
                oldToken.RevokedByIp = ipAddress;
                
                // Generate a new refresh token
                var newRefreshToken = GenerateRefreshToken(ipAddress);
                newRefreshToken.UserId = user.Id;
                oldToken.ReplacedByToken = newRefreshToken.Token;
                
                // Add the new token directly to the database
                _unitOfWork.RefreshTokens.AddAsync(newRefreshToken).GetAwaiter().GetResult();
                
                // Save the changes
                _unitOfWork.CompleteAsync().GetAwaiter().GetResult();
                
                _logger.LogInformation("Refreshed token for user {UserId} ({Email}): old token {OldToken} replaced with {NewToken}", 
                    user.Id, user.Email, refreshToken.Substring(0, 10), newRefreshToken.Token.Substring(0, 10));
                
                // Generate a new JWT token
                var token = GenerateJwtToken(user);
                    
                return new AuthenticationResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    SystemRole = user.SystemRole,
                    Token = token,
                    RefreshToken = newRefreshToken.Token,
                    RefreshTokenExpiration = newRefreshToken.Expires
                };
            }
            catch (OpenAutomateException)
            {
                // Rethrow custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token: {Message}", ex.Message);
                throw new ServiceException($"Error refreshing token: {ex.Message}", ex);
            }
        }

        public Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = "")
        {
            try
            {
                _logger.LogDebug("Revoking token {Token}", token.Substring(0, 10));
                
                // Find the token directly in the database without the computed property
                var refreshToken = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Token == token).GetAwaiter().GetResult();
                    
                if (refreshToken == null || refreshToken.IsRevoked)
                {
                    _logger.LogWarning("Token not found or already revoked: {Token}", token.Substring(0, 10));
                    return Task.FromResult(false);
                }
                
                // Revoke the token
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                refreshToken.ReasonRevoked = reason;
                
                // Save the changes
                _unitOfWork.CompleteAsync().GetAwaiter().GetResult();
                
                _logger.LogInformation("Successfully revoked token for user {UserId}", refreshToken.UserId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return Task.FromResult(false);
            }
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            
            // Get the current tenant if available
            Guid? tenantId = _tenantContext.HasTenant ? _tenantContext.CurrentTenantId : null;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.SystemRole.ToString())
            };

            // Add tenant claim if there is a tenant context
            if (tenantId.HasValue)
            {
                claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt= DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private async Task AddRefreshTokenToUserAsync(User user, RefreshToken refreshToken)
        {
            try
            {
                _logger.LogDebug("Adding refresh token to user {UserId}", user.Id);
                
                // Explicitly set both the UserId and User properties
                refreshToken.UserId = user.Id;
                refreshToken.User = user;
                
                // Add the refresh token directly to the RefreshTokens repository
                await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
                
                // Save changes to persist the refresh token
                await _unitOfWork.CompleteAsync();
                
                // Verify the token was saved with the correct UserId
                var savedToken = await _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Id == refreshToken.Id);
                
                if (savedToken != null)
                {
                    _logger.LogInformation("Token {TokenId} successfully added for user {UserId}", 
                        refreshToken.Id, user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to verify token {TokenId} was saved properly for user {UserId}",
                        refreshToken.Id, user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token to user {UserId}: {Message}", user.Id, ex.Message);
                throw new ServiceException($"Error adding refresh token to user {user.Id}: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId)
        {
            try
            {
                // Check if user exists
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new AuthenticationException($"User with ID {userId} not found");
                }

                // Remove any existing verification tokens for this user
                var existingTokens = await _unitOfWork.EmailVerificationTokens
                    .GetAllAsync(t => t.UserId == userId && !t.IsUsed);

                foreach (var token in existingTokens)
                {
                    _unitOfWork.EmailVerificationTokens.Remove(token);
                }
                await _unitOfWork.CompleteAsync();

                // Generate a new token
                using var rng = RandomNumberGenerator.Create();
                var randomBytes = new byte[32];
                rng.GetBytes(randomBytes);
                var tokenString = Convert.ToBase64String(randomBytes)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Replace("=", "");

                // Create token entity
                var verificationToken = new EmailVerificationToken
                {
                    UserId = userId,
                    Token = tokenString,
                    ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hour expiration
                    IsUsed = false
                };

                // Save to database
                await _unitOfWork.EmailVerificationTokens.AddAsync(verificationToken);
                await _unitOfWork.CompleteAsync();

                return tokenString;
            }
            catch (OpenAutomateException)
            {
                // Rethrow custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email verification token: {Message}", ex.Message);
                throw new ServiceException($"Error generating email verification token: {ex.Message}", ex);
            }
        }

        public async Task<Guid?> ValidateEmailVerificationTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                // Find the token in the database
                var verificationToken = await _unitOfWork.EmailVerificationTokens
                    .GetFirstOrDefaultAsync(t => t.Token == token, t => t.User);

                // Check if token exists
                if (verificationToken == null)
                    return null;

                // Check if token is already used
                if (verificationToken.IsUsed)
                    return null;

                // Check if token is expired
                if (verificationToken.IsExpired)
                    return null;

                // Mark token as used
                verificationToken.IsUsed = true;
                verificationToken.UsedAt = DateTime.UtcNow;
                _unitOfWork.EmailVerificationTokens.Update(verificationToken);
                await _unitOfWork.CompleteAsync();

                return verificationToken.UserId;
            }
            catch (OpenAutomateException)
            {
                // Rethrow custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email verification token: {Message}", ex.Message);
                return null;
            }
        }
    }
} 