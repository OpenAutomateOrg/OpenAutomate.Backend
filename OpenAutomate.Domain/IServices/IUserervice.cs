using OpenAutomate.Domain.Dto;

namespace OpenAutomate.Domain.IServices
{
    public interface IUserervice
    {
        void Authenticate(AuthenticateRequest model);

    }
}
