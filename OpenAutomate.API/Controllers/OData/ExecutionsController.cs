using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using System.Linq;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Executions
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Executions
    /// </remarks>
    [Route("{tenant}/odata/Executions")]
    [ApiController]
    [Authorize]
    public class ExecutionsController : ODataController
    {
        private readonly IExecutionService _executionService;
        private readonly ILogger<ExecutionsController> _logger;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionsController"/> class
        /// </summary>
        /// <param name="executionService">The execution service</param>
        /// <param name="logger">The logger</param>
        /// <param name="tenantContext">The tenant context</param>
        public ExecutionsController(
            IExecutionService executionService,
            ILogger<ExecutionsController> logger,
            ITenantContext tenantContext)
        {
            _executionService = executionService;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets all Executions with OData query support
        /// </summary>
        /// <returns>Collection of Executions that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/Executions?$filter=Status eq 'Running'
        /// GET /tenant/odata/Executions?$select=Id,Status,StartTime,EndTime
        /// GET /tenant/odata/Executions?$expand=BotAgent,Package
        /// GET /tenant/odata/Executions?$filter=Status eq 'Completed' and StartTime gt 2024-01-01T00:00:00Z
        /// GET /tenant/odata/Executions?$filter=contains(BotAgent/Name,'Agent1') and Status ne 'Failed'
        /// </remarks>
        [HttpGet]
        [RequirePermission(Resources.ExecutionResource, Permissions.View)]
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
                    
                    // Let the service handle tenant resolution
                    var tenantResolved = await _tenantContext.ResolveTenantFromSlugAsync(tenantSlug);
                    if (!tenantResolved)
                    {
                        _logger.LogError("Failed to resolve tenant from route: {TenantSlug}", tenantSlug);
                        return NotFound($"Tenant '{tenantSlug}' not found or inactive");
                    }
                }

                var executions = await _executionService.GetAllExecutionsAsync();

                // Transform to DTOs for OData
                var executionDtos = executions.Select(execution => new ExecutionResponseDto
                {
                    Id = execution.Id,
                    BotAgentId = execution.BotAgentId,
                    PackageId = execution.PackageId,
                    Status = execution.Status,
                    StartTime = execution.StartTime,
                    EndTime = execution.EndTime,
                    ErrorMessage = execution.ErrorMessage,
                    LogOutput = execution.LogOutput,
                    HasLogs = !string.IsNullOrEmpty(execution.LogS3Path),
                    BotAgentName = execution.BotAgent?.Name,
                    PackageName = execution.Package?.Name,
                    PackageVersion = execution.Package?.Versions?.FirstOrDefault()?.VersionNumber
                });

                return Ok(executionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving executions for OData query");
                return StatusCode(500, "An error occurred while retrieving executions");
            }
        }

        /// <summary>
        /// Gets a specific Execution by ID with OData query support
        /// </summary>
        /// <param name="key">The Execution ID</param>
        /// <returns>The Execution if found</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/Executions(guid'12345678-1234-1234-1234-123456789012')?$select=Status,StartTime,EndTime
        /// GET /tenant/odata/Executions(guid'12345678-1234-1234-1234-123456789012')?$expand=BotAgent,Package
        /// </remarks>
        [HttpGet("{key}")]
        [RequirePermission(Resources.ExecutionResource, Permissions.View)]
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
                    
                    // Let the service handle tenant resolution
                    var tenantResolved = await _tenantContext.ResolveTenantFromSlugAsync(tenantSlug);
                    if (!tenantResolved)
                    {
                        _logger.LogError("Failed to resolve tenant from route: {TenantSlug}", tenantSlug);
                        return NotFound($"Tenant '{tenantSlug}' not found or inactive");
                    }
                }

                var execution = await _executionService.GetExecutionByIdAsync(key);
                if (execution == null)
                    return NotFound();

                // Transform to DTO for OData
                var executionDto = new ExecutionResponseDto
                {
                    Id = execution.Id,
                    BotAgentId = execution.BotAgentId,
                    PackageId = execution.PackageId,
                    Status = execution.Status,
                    StartTime = execution.StartTime,
                    EndTime = execution.EndTime,
                    ErrorMessage = execution.ErrorMessage,
                    LogOutput = execution.LogOutput,
                    HasLogs = !string.IsNullOrEmpty(execution.LogS3Path),
                    BotAgentName = execution.BotAgent?.Name,
                    PackageName = execution.Package?.Name,
                    PackageVersion = execution.Package?.Versions?.FirstOrDefault()?.VersionNumber
                };

                return Ok(executionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving execution with ID {ExecutionId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the execution");
            }
        }
    }
} 