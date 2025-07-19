using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Authority;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Authorities (Roles)
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Authorities/Roles within a tenant context.
    /// All data is automatically filtered by the current tenant (organization unit).
    /// </remarks>
    [Route("{tenant}/odata/Roles")]
    [ApiController]
    [Authorize]
    public class AuthoritiesController : ODataController
    {
        private readonly IAuthorizationManager _authorizationManager;
        private readonly ILogger<AuthoritiesController> _logger;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthoritiesController"/> class
        /// </summary>
        /// <param name="authorizationManager">The authorization manager service</param>
        /// <param name="logger">The logger</param>
        /// <param name="tenantContext">The tenant context</param>
        public AuthoritiesController(
            IAuthorizationManager authorizationManager,
            ILogger<AuthoritiesController> logger,
            ITenantContext tenantContext)
        {
            _authorizationManager = authorizationManager;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets all Authorities (Roles) with OData query support
        /// </summary>
        /// <returns>Collection of Authorities that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /{tenant}/odata/Roles?$filter=IsSystemAuthority eq false
        /// GET /{tenant}/odata/Roles?$select=Id,Name,Description
        /// GET /{tenant}/odata/Roles?$expand=Permissions
        /// GET /{tenant}/odata/Roles?$filter=contains(Name,'Admin')
        /// </remarks>
        [HttpGet]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [EnableQuery]
        [EnableResponseCache(900)] // Cache for 15 minutes - authorities change less frequently
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
                    
                    _logger.LogWarning("Tenant context not set for authorities query, tenant: {TenantSlug}", tenantSlug);
                    return BadRequest("Tenant context not properly initialized");
                }

                var authorities = await _authorizationManager.GetAllAuthoritiesWithPermissionsAsync();
                
                _logger.LogInformation("Retrieved {Count} authorities for tenant {TenantId}", 
                    authorities.Count(), _tenantContext.CurrentTenantId);
                
                return Ok(authorities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authorities for OData query in tenant {TenantId}", 
                    _tenantContext.CurrentTenantId);
                return StatusCode(500, "An error occurred while retrieving authorities");
            }
        }

        /// <summary>
        /// Gets a specific Authority (Role) by ID with OData query support
        /// </summary>
        /// <param name="key">The Authority ID</param>
        /// <returns>The Authority if found</returns>
        /// <remarks>
        /// Example query:
        /// GET /{tenant}/odata/Roles(guid'12345678-1234-1234-1234-123456789012')?$select=Name,Description
        /// GET /{tenant}/odata/Roles(guid'12345678-1234-1234-1234-123456789012')?$expand=Permissions
        /// </remarks>
        [HttpGet("{key}")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        [EnableQuery]
        [EnableResponseCache(1800)] // Cache for 30 minutes - individual authorities are stable
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                // Check if tenant context is available
                if (!_tenantContext.HasTenant)
                {
                    var tenantSlug = RouteData.Values["tenant"]?.ToString();
                    if (string.IsNullOrEmpty(tenantSlug))
                    {
                        _logger.LogError("Tenant slug not available in route data");
                        return BadRequest("Tenant not specified");
                    }
                    
                    _logger.LogWarning("Tenant context not set for authority query, tenant: {TenantSlug}, authorityId: {AuthorityId}", 
                        tenantSlug, key);
                    return BadRequest("Tenant context not properly initialized");
                }

                var authority = await _authorizationManager.GetAuthorityWithPermissionsAsync(key);
                if (authority == null)
                {
                    _logger.LogWarning("Authority with ID {AuthorityId} not found in tenant {TenantId}", 
                        key, _tenantContext.CurrentTenantId);
                    return NotFound();
                }

                _logger.LogInformation("Retrieved authority {AuthorityId} ({AuthorityName}) for tenant {TenantId}", 
                    authority.Id, authority.Name, _tenantContext.CurrentTenantId);
                    
                return Ok(authority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authority with ID {AuthorityId} for OData query in tenant {TenantId}", 
                    key, _tenantContext.CurrentTenantId);
                return StatusCode(500, "An error occurred while retrieving the authority");
            }
        }
    }   
} 