using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Infrastructure.DbContext;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(user => new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    IsEmailVerified = user.IsEmailVerified,
                    SystemRole = user.SystemRole,
                     CreatedBy = user.CreatedBy, 
                    CreatedAt = user.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
