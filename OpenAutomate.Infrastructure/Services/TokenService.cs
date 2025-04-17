using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
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

namespace OpenAutomate.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public AuthenticationResponse GenerateTokens(User user, string ipAddress)
        {
            try
            {
                // Generate JWT token
                var token = GenerateJwtToken(user);

                // Generate refresh token
                var refreshToken = GenerateRefreshToken(ipAddress);
                
                // Add refresh token to user
                AddRefreshTokenToUserAsync(user, refreshToken).GetAwaiter().GetResult();

                return new AuthenticationResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Token = token,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiration = refreshToken.Expires
                };
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error saving refresh token: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthenticationResponse> RefreshToken(string refreshToken, string ipAddress)
        {
            try
            {
                // First find the token in the database without the computed properties
                var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                    t => t.RefreshTokens.Any(x => x.Token == refreshToken), 
                    u => u.RefreshTokens);
                    
                var oldToken = user.RefreshTokens.FirstOrDefault(x => x.Token == refreshToken);
                
                if (user == null)
                    throw new Exception("User not found");
                    
                if (oldToken == null)
                    throw new Exception("Invalid token");

                if (oldToken.IsRevoked)
                {
                    await RevokeDecendantRefreshToken(oldToken, user, ipAddress, $"Attempt reuse of revoked ancestor token: {oldToken}");
                    _unitOfWork.RefreshTokens.Update(oldToken);
                }
                
                // Then check the computed properties in memory
                if (oldToken.IsExpired)
                    throw new Exception("Token is expired");
 
                var newRefreshToken = await RotateRefreshToken(oldToken, ipAddress);
                
                // Set the user ID for the new refresh token
                newRefreshToken.UserId = user.Id;
                
                // Add the new refresh token directly
                await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);
                
                // Save changes
                await _unitOfWork.CompleteAsync();
                
                // Generate a new JWT token
                var token = GenerateJwtToken(user);
                    
                return new AuthenticationResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Token = token,
                    RefreshToken = newRefreshToken.Token,
                    RefreshTokenExpiration = newRefreshToken.Expires
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing token: {ex.Message}");
                throw;
            }
        }

        public async Task<RefreshToken> RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = GenerateRefreshToken(ipAddress);
            RevokeRefreshToken(refreshToken, ipAddress, "Request replace a new refresh token", newRefreshToken.Token).GetAwaiter().GetResult();
            return newRefreshToken; 
        }

        public async Task RevokeRefreshToken(RefreshToken refreshToken, string ipAddress, string reason = null,
            string replacedByToken = null)
        {
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = replacedByToken;
            refreshToken.ReasonRevoked = reason;
        }
        
        
        // TODO: add RevokeRefreshToken and remove unnecessary code 
        public bool RevokeToken(string token, string ipAddress, string reason = null)
        {
            try
            {
                // Find the token directly in the database without the computed property
                var refreshToken = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Token == token).GetAwaiter().GetResult();
                    
                if (refreshToken == null || refreshToken.IsRevoked || !refreshToken.IsActive)
                    throw new Exception("Invalid token");
                    
                // Revoke the token
                RevokeRefreshToken(refreshToken, ipAddress, reason).GetAwaiter().GetResult();   
                
                // Save the changes
                _unitOfWork.CompleteAsync().GetAwaiter().GetResult();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error revoking token: {ex.Message}");
                return false;
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
                }, out SecurityToken validatedToken);

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
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private async Task AddRefreshTokenToUserAsync(User user, RefreshToken refreshToken)
        {
            // Set the UserId on the refresh token
            refreshToken.UserId = user.Id;
            
            // Add the refresh token directly to the RefreshTokens repository
            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            
            // Save changes
            await _unitOfWork.CompleteAsync();
        }

        
        public async Task RevokeDecendantRefreshToken(RefreshToken refreshToken, User user, string ipAddress, string reason = null)
        {
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var childToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);

                if (childToken.IsActive)
                {
                    RevokeToken(childToken.ReplacedByToken, ipAddress, reason);
                }
                else
                {
                    RevokeDecendantRefreshToken(childToken, user, ipAddress, reason);
                }
            }
        }
    }
} 