using AutoMapper;
using Azure;
using OpenAutomate.Domain.Constants;
using OpenAutomate.Domain.Dto.UserDto;
using OpenAutomate.Domain.Entities;
using OpenAutomate.Domain.Interfaces.IJwtUtils;
using OpenAutomate.Domain.Interfaces.IRepository;
using OpenAutomate.Domain.Interfaces.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    public class UserSerivce : IUserservice
    {
        private readonly IRepository<User> _userRepository;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;

        public UserSerivce(IRepository<User> userRepository, IJwtUtils jwtUtils, IMapper mapper)
        {
            _userRepository = userRepository;
            _jwtUtils = jwtUtils;
            _mapper = mapper;

        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticateRequest model)
        {
            var account = await _userRepository.GetFirstOrDefaultAsync(x => x.Email == model.Email);

            // validation : will check whether user with email is correct or not 
            if (account == null || !BCrypt.Net.BCrypt.Verify(model.Password, account.PasswordHash))
            {
                // TODO: Should create a new Middleware Exeption for handle exeption of System. 
                throw new Exception("Email or password is incorrect");
            }

            // tao JWT token 

            var jwtToken = _jwtUtils.GenerateJwtToken(account); 
            var refreshToken = _jwtUtils.GenerateRefreshToken("");
            account.RefreshTokens.Add(refreshToken);

            // tao refresh token 
            // xoa token cu 
            RemoveOldRefreshToken(account); 

            _userRepository.Update(account);
            await _userRepository.SaveChangesAsync();

            var response = _mapper.Map<AuthenticationResponse>(account);

            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            
            return response;    
            




        }

        // xoa token cu -- Time < 3 ngay, !isActive

        public void RemoveOldRefreshToken(User user)
        {
            if (user.RefreshTokens == null || user.RefreshTokens.Count() == 0) return; 

            user.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(AppSettings.RefreshTokenTTL) <= DateTime.UtcNow); 
        }

      


    }
}
