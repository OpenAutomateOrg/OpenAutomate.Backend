using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using System.Security.Claims;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<ExternalAuthController> _logger;

        // Standard claim type constants
        private const string EmailClaimType = ClaimTypes.Email;
        private const string FirstNameClaimType = ClaimTypes.GivenName;
        private const string LastNameClaimType = ClaimTypes.Surname;

        public ExternalAuthController(IUserService userService, ILogger<ExternalAuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates Google OAuth login flow
        /// </summary>
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse)),
                // Add allowed error parameter to be returned to callback
                AllowRefresh = true
            };
            
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the Google OAuth response and creates/authenticates user
        /// </summary>
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    return Unauthorized("Authentication failed");
                }

                // Extract user information from Google claims
                var claimsPrincipal = result.Principal;
                var email = claimsPrincipal?.FindFirstValue(EmailClaimType);
                var firstName = claimsPrincipal?.FindFirstValue(FirstNameClaimType);
                var lastName = claimsPrincipal?.FindFirstValue(LastNameClaimType);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                {
                    _logger.LogWarning("Required claims are missing in the authentication response");
                    return BadRequest("Missing required user information from Google authentication");
                }

                // Process token or register user if needed
                var user = await _userService.GetByEmailAsync(email);
                var ipAddress = GetIpAddress();
                
                if (user == null)
                {
                    _logger.LogInformation("Registering new user from Google authentication: {Email}", email);
                    
                    var registrationRequest = new RegistrationRequest
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        // Generate secure random password - user won't need to know this as they'll use Google login
                        Password = Guid.NewGuid().ToString("N"),
                        ConfirmPassword = Guid.NewGuid().ToString("N")
                    };
                    
                    user = await _userService.RegisterAsync(registrationRequest, ipAddress);
                }

                // Generate token for the user
                var authenticationResponse = await _userService.AuthenticateAsync(
                    new AuthenticationRequest { Email = user.Email, Password = null }, ipAddress);
                
                // Sign out of cookie auth since we're using JWT for API access
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Ok(authenticationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication process");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during authentication");
            }
        }

        /// <summary>
        /// Gets the client IP address from request headers or connection information
        /// </summary>
        private string GetIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && forwardedFor.Count > 0)
            {
                return forwardedFor.ToString().Split(',')[0].Trim();
            }
            
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        }
    }
}
