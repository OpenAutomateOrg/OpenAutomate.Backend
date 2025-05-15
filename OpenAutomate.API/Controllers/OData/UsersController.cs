using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Users
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Users
    /// Only accessible by administrators
    /// </remarks>
    [Route("{tenant}/odata/Users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ODataController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<UsersController> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class
        /// </summary>
        /// <param name="adminService">The admin service</param>
        /// <param name="logger">The logger</param>
        public UsersController(IAdminService adminService, ILogger<UsersController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets all Users with OData query support
        /// </summary>
        /// <returns>Collection of Users that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/Users?$filter=IsActive eq true
        /// GET /tenant/odata/Users?$orderby=Email asc&$top=10&$skip=10
        /// GET /tenant/odata/Users?$select=Id,Email,FirstName,LastName
        /// </remarks>
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for OData query");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }
        
        /// <summary>
        /// Gets a specific User by ID with OData query support
        /// </summary>
        /// <param name="key">The User ID</param>
        /// <returns>The User if found</returns>
        /// <remarks>
        /// Example query:
        /// GET /tenant/odata/Users(guid'12345678-1234-1234-1234-123456789012')?$select=Email,FirstName,LastName
        /// </remarks>
        [HttpGet("{key}")]
        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(key);
                if (user == null)
                    return NotFound();
                    
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }
    }
} 