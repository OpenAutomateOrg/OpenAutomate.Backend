using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpenAutomate.Domain.Constants;
using OpenAutomate.Domain.Entities;
using OpenAutomate.Domain.Interfaces.IJwtUtils;
using OpenAutomate.Infrastructure.DbContext;

namespace OpenAutomate.Infrastructure.Services
{
    public class JwtUtils : IJwtUtils
    {

        private readonly ApplicationDbContext _context;

        public JwtUtils(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GenerateJwtToken(User user)
        {
            // generate token that is valid for 15 minutes
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AppSettings.Secret);
            // Description of Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                // Time Expire
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            // Get Token and return 
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var refreshToken = new RefreshToken
            {
                // token is a cryptographically strong random sequence of values
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)),
                // token is valid for 7 days
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            // ensure token is unique by checking against db
            var tokenIsUnique = !_context.Users.Any(a => a.RefreshTokens.Any(t => t.Token == refreshToken.Token));

            if (!tokenIsUnique)
                return GenerateRefreshToken(ipAddress);

            return refreshToken;
        }

        public bool ValidateJwtToken(string token, out string? userId)
        {
            userId = null;
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppSettings.Secret);

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Change if you have an issuer
                    ValidateAudience = false, // Change if you have an audience
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    // Extract the "id" claim
                    userId = principal.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                    return !string.IsNullOrEmpty(userId);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}
