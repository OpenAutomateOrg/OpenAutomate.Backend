using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Automation Packages
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Automation Packages
    /// </remarks>
    [Route("{tenant}/odata/AutomationPackages")]
    [ApiController]
    [Authorize]
    public class AutomationPackagesController : ODataController
    {
        private readonly IAutomationPackageService _packageService;
        private readonly ILogger<AutomationPackagesController> _logger;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationPackagesController"/> class
        /// </summary>
        /// <param name="packageService">The automation package service</param>
        /// <param name="logger">The logger</param>
        /// <param name="tenantContext">The tenant context</param>
        public AutomationPackagesController(
            IAutomationPackageService packageService,
            ILogger<AutomationPackagesController> logger,
            ITenantContext tenantContext)
        {
            _packageService = packageService;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets all Automation Packages with OData query support
        /// </summary>
        /// <returns>Collection of Automation Packages that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/AutomationPackages?$filter=IsActive eq true
        /// GET /tenant/odata/AutomationPackages?$select=Id,Name,Description,CreatedAt
        /// GET /tenant/odata/AutomationPackages?$expand=Versions
        /// GET /tenant/odata/AutomationPackages?$filter=contains(Name,'automation') and CreatedAt gt 2024-01-01T00:00:00Z
        /// </remarks>
        [HttpGet]
        [RequirePermission(Resources.PackageResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Check if tenant context is available, if not try to resolve from route
                if (!_tenantContext.HasTenant)
                {
                    var tenantSlug = RouteData.Values["tenant"]?.ToString();
                    if (string.IsNullOrEmpty(tenantSlug))
                    {
                        _logger.LogError("Tenant slug not available in route data");
                        return BadRequest("Tenant not specified");
                    }
                    
                    _logger.LogWarning("Tenant context not set, attempting to resolve from route: {TenantSlug}", tenantSlug);
                }

                var packages = await _packageService.GetAllPackagesAsync();
                return Ok(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automation packages for OData query");
                return StatusCode(500, "An error occurred while retrieving automation packages");
            }
        }

        /// <summary>
        /// Gets a specific Automation Package by ID with OData query support
        /// </summary>
        /// <param name="key">The Automation Package ID</param>
        /// <returns>The Automation Package if found</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/AutomationPackages(guid'12345678-1234-1234-1234-123456789012')?$select=Name,Description
        /// GET /tenant/odata/AutomationPackages(guid'12345678-1234-1234-1234-123456789012')?$expand=Versions
        /// </remarks>
        [HttpGet("{key}")]
        [RequirePermission(Resources.PackageResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                // Check if tenant context is available, if not try to resolve from route
                if (!_tenantContext.HasTenant)
                {
                    var tenantSlug = RouteData.Values["tenant"]?.ToString();
                    if (string.IsNullOrEmpty(tenantSlug))
                    {
                        _logger.LogError("Tenant slug not available in route data");
                        return BadRequest("Tenant not specified");
                    }
                    
                    _logger.LogWarning("Tenant context not set, attempting to resolve from route: {TenantSlug}", tenantSlug);
                }

                var package = await _packageService.GetPackageByIdAsync(key);
                if (package == null)
                    return NotFound();

                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving automation package with ID {PackageId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the automation package");
            }
        }
    }
} 