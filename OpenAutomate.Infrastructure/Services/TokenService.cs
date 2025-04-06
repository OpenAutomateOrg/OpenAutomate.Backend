using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;

        public TokenService(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext)
        {
            _configuration = configuration;
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

        public AuthenticationResponse RefreshToken(string refreshToken, string ipAddress)
        {
            try
            {
                // First find the token in the database without the computed properties
                var oldToken = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Token == refreshToken,
                    t => t.User).GetAwaiter().GetResult();
                    
                if (oldToken == null)
                    throw new Exception("Invalid token");
                    
                // Then check the computed properties in memory
                if (oldToken.IsRevoked || oldToken.IsExpired)
                    throw new Exception("Token is revoked or expired");
                    
                var user = oldToken.User;
                if (user == null)
                    throw new Exception("User not found");
                    
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

        public bool RevokeToken(string token, string ipAddress, string reason = null)
        {
            try
            {
                // Find the token directly in the database without the computed property
                var refreshToken = _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                    t => t.Token == token).GetAwaiter().GetResult();
                    
                if (refreshToken == null || refreshToken.IsRevoked)
                    return false;
                    
                // Revoke the token
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                refreshToken.ReasonRevoked = reason;
                
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
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
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
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            
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
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
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
                Expires = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"])),
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
    }
} 