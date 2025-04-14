using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("{ou}/api/author")]
    [Authorize]
    public class AuthorController : CustomControllerBase
    {
        private readonly IAuthorizationManager _authorizationManager;
        
        public AuthorController(IAuthorizationManager authorizationManager)
        {
            _authorizationManager = authorizationManager;
        }
        
        [HttpGet("user/{userId}")]
        [RequirePermission(Resources.AdminResource, Permissions.View)]
        public async Task<IActionResult> GetUserAuthorities(Guid userId)
        {
            var authorities = await _authorizationManager.GetUserAuthoritiesAsync(userId);
            var result = authorities.Select(a => new AuthorityDto { Name = a.Name });
            return Ok(result);
        }
        
        [HttpPost("user/{userId}")]
        [RequirePermission(Resources.AdminResource, Permissions.Update)]
        public async Task<IActionResult> AssignAuthorityToUser(Guid userId, [FromBody] AssignAuthorityDto dto)
        {
            await _authorizationManager.AssignAuthorityToUserAsync(userId, dto.AuthorityName);
            return Ok();
        }
        
        [HttpDelete("user/{userId}/{authorityName}")]
        [RequirePermission(Resources.AdminResource, Permissions.Delete)]
        public async Task<IActionResult> RemoveAuthorityFromUser(Guid userId, string authorityName)
        {
            await _authorizationManager.RemoveAuthorityFromUserAsync(userId, authorityName);
            return Ok();
        }
        
        [HttpPost("permission")]
        [RequirePermission(Resources.AdminResource, Permissions.Create)]
        public async Task<IActionResult> AddResourcePermission([FromBody] ResourcePermissionDto dto)
        {
            await _authorizationManager.AddResourcePermissionAsync(
                dto.AuthorityName,
                dto.ResourceName,
                dto.Permission
            );
            return Ok();
        }
        
        [HttpDelete("permission/{authorityName}/{resourceName}")]
        [RequirePermission(Resources.AdminResource, Permissions.Delete)]
        public async Task<IActionResult> RemoveResourcePermission(string authorityName, string resourceName)
        {
            await _authorizationManager.RemoveResourcePermissionAsync(authorityName, resourceName);
            return Ok();
        }
    }
} 