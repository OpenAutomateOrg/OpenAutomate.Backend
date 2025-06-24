using System;
using System.Linq;
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
    /// OData controller for querying Package Versions
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Package Versions across all packages
    /// </remarks>
    [Route("{tenant}/odata/PackageVersions")]
    [ApiController]
    [Authorize]
    public class PackageVersionsController : ODataController
    {
        private readonly IAutomationPackageService _packageService;
        private readonly ILogger<PackageVersionsController> _logger;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageVersionsController"/> class
        /// </summary>
        /// <param name="packageService">The automation package service</param>
        /// <param name="logger">The logger</param>
        /// <param name="tenantContext">The tenant context</param>
        public PackageVersionsController(
            IAutomationPackageService packageService,
            ILogger<PackageVersionsController> logger,
            ITenantContext tenantContext)
        {
            _packageService = packageService;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets all Package Versions with OData query support
        /// </summary>
        /// <returns>Collection of Package Versions that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/PackageVersions?$filter=IsActive eq true
        /// GET /tenant/odata/PackageVersions?$select=Id,VersionNumber,FileName,FileSize,UploadedAt
        /// GET /tenant/odata/PackageVersions?$filter=FileSize gt 1000000 and UploadedAt gt 2024-01-01T00:00:00Z
        /// GET /tenant/odata/PackageVersions?$filter=contains(VersionNumber,'1.0') and ContentType eq 'application/zip'
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

                // Get all packages and extract their versions
                var packages = await _packageService.GetAllPackagesAsync();
                var allVersions = packages.SelectMany(p => p.Versions).ToList();
                
                return Ok(allVersions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package versions for OData query");
                return StatusCode(500, "An error occurred while retrieving package versions");
            }
        }

        /// <summary>
        /// Gets a specific Package Version by ID with OData query support
        /// </summary>
        /// <param name="key">The Package Version ID</param>
        /// <returns>The Package Version if found</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/PackageVersions(guid'12345678-1234-1234-1234-123456789012')?$select=VersionNumber,FileSize
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

                // Get all packages and find the specific version
                var packages = await _packageService.GetAllPackagesAsync();
                var version = packages.SelectMany(p => p.Versions)
                                    .FirstOrDefault(v => v.Id == key);
                
                if (version == null)
                    return NotFound();

                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package version with ID {VersionId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the package version");
            }
        }
    }
} 