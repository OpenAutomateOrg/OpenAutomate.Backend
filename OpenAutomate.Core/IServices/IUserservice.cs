using OpenAutomate.Core.Dto.UserDto;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IUserService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress);
        Task<AuthenticationResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task<bool> RevokeTokenAsync(string token, string ipAddress, string reason = null);
        Task<UserResponse> RegisterAsync(RegistrationRequest request, string ipAddress);
        Task<UserResponse> GetByIdAsync(Guid id);
    }
}
