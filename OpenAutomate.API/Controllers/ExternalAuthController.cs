using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<ExternalAuthController> _logger;


        public ExternalAuthController(IUserService userService, ILogger<ExternalAuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/ExternalAuth/google-response"
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Unauthorized();

            // Lấy thông tin người dùng từ Google
            var claims = result.Principal?.Identities
                .FirstOrDefault()?.Claims
                .ToDictionary(c => c.Type, c => c.Value);

            var email = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"];
            var firstName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"];
            var lastName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"];

            // Xử lý token hoặc đăng ký người dùng
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                var registrationRequest = new RegistrationRequest
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Password = Guid.NewGuid().ToString(),
                    ConfirmPassword = Guid.NewGuid().ToString()
                };
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                user = await _userService.RegisterAsync(registrationRequest, ipAddress);
            }

            // Tạo token cho người dùng
            var ipAddressForLogin = HttpContext.Connection.RemoteIpAddress?.ToString();
            var authenticationResponse = await _userService.AuthenticateAsync(
                new AuthenticationRequest { Email = user.Email, Password = null }, ipAddressForLogin);

            return Ok(authenticationResponse);
        }
    }
}
