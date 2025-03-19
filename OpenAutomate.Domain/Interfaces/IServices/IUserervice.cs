using OpenAutomate.Domain.Dto;

namespace OpenAutomate.Domain.Interfaces.IServices
{
    public interface IUserervice
    {
        void Authenticate(AuthenticateRequest model);

    }
}
