using Microsoft.Identity.Client;
using OpenAutomate.Domain.Dto;
using OpenAutomate.Domain.Entities;
using OpenAutomate.Domain.Interfaces.IRepository;
using OpenAutomate.Domain.Interfaces.IServices;

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
            if (account == null || !BCrypt.Net.BCrypt.Verify(model.Password, account.PasswordHash)) 
            {
                // TODO: Should create a new Middleware Exeption for handle exeption of System. 
                throw new Exception("Email or password is incorrect");
            } 

            


        }
    }
}
