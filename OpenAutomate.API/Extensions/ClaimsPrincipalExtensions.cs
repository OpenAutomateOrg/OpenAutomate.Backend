using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace OpenAutomate.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Lấy ID người dùng từ claims
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>ID người dùng dạng Guid, hoặc Guid.Empty nếu không tìm thấy</returns>
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            // Tìm trong claim "sub" (JWT standard subject claim)
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub) ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            
            if (subClaim != null && Guid.TryParse(subClaim.Value, out Guid userId))
            {
                return userId;
            }
            
            return Guid.Empty;
        }
    }
} 