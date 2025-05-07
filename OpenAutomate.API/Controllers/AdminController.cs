using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.IServices;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : CustomControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all user details.
        /// </summary>
        /// <returns>A list of users.</returns>
        [HttpGet("users/all-users")]
        public async Task<IActionResult> GetAllUserDetails()
        {
       
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
        
        }
    }
}
