using OpenAutomate.Domain.Dto;

namespace OpenAutomate.Domain.Interfaces.IServices
{
    public interface IUserservice
    {
        void Authenticate(AuthenticateRequest model);

    }
}
