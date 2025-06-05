using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Organization Unit Users
    /// </summary>
    [Route("{tenant}/odata/OrganizationUnitUsers")]
    [ApiController]
    [Authorize]
    public class OrganizationUnitUsersController : ODataController
    {
        private readonly IOrganizationUnitUserService _service;
        private readonly ILogger<OrganizationUnitUsersController> _logger;
        private readonly ITenantContext _tenantContext;

        public OrganizationUnitUsersController(
            IOrganizationUnitUserService service,
            ILogger<OrganizationUnitUsersController> logger,
            ITenantContext tenantContext)
        {
            _service = service;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets all Organization Unit Users with OData query support
        /// </summary>
        /// <returns>Collection of OrganizationUnitUserDetailDto</returns>
        [HttpGet]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                string? tenantSlug = _tenantContext.CurrentTenantSlug;
                if (string.IsNullOrEmpty(tenantSlug))
                {
                    tenantSlug = RouteData.Values["tenant"]?.ToString();
                }
                if (string.IsNullOrEmpty(tenantSlug))
                {
                    _logger.LogError("Tenant slug not available in context or route data");
                    return BadRequest("Tenant not specified");
                }

                var users = await _service.GetUsersInOrganizationUnitAsync(tenantSlug);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OU users for OData query");
                return StatusCode(500, "An error occurred while retrieving OU users");
            }
        }
    }
}
