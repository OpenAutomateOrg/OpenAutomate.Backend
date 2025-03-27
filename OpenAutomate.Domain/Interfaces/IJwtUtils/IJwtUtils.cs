using OpenAutomate.Domain.Entities;

namespace OpenAutomate.Domain.Interfaces.IJwtUtils
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(User user);
        public RefreshToken GenerateRefreshToken(string ipAddress);
        public bool ValidateJwtToken(string token, out string? userId);
    }
}
