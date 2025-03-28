using OpenAutomate.Domain.Dto.UserDto;

namespace OpenAutomate.Domain.Interfaces.IServices
{
    public interface IUserservice
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticateRequest model);

    }
}
