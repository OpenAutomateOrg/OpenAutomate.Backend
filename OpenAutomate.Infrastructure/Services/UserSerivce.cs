using Microsoft.Identity.Client;
using OpenAutomate.Domain.Dto;
using OpenAutomate.Domain.Entities;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Domain.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    public class UserSerivce : IUserervice
    {
        public IRepository<User> _userRepository;
        public UserSerivce(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async void Authenticate(AuthenticateRequest model)
        {
            var account = await _userRepository.GetFirstOrDefaultAsync(x => x.Email == model.Email);

            // validation : will check whether user with email is correct or not 
            if (account == null) 
            {
                return;
            } 




        }
    }
}
