using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Statistics;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for system-wide statistics
    /// </summary>
    [Route("api/system/statistics")]
    [ApiController]
    [Authorize]
    public class SystemStatisticsController : ControllerBase
    {
        private readonly ISystemStatisticsService _systemStatisticsService;
        private readonly ILogger<SystemStatisticsController> _logger;

        public SystemStatisticsController(
            ISystemStatisticsService systemStatisticsService,
            ILogger<SystemStatisticsController> logger)
        {
            _systemStatisticsService = systemStatisticsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets total resource counts across all organization units in the system
        /// </summary>
        /// <returns>System-wide resource summary with total counts</returns>
        /// <response code="200">Resource summary retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User lacks required permissions</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("resources")]
        [RequirePermission(Resources.OrganizationUnitResource, Permissions.View)]
        public async Task<ActionResult<SystemResourceSummaryDto>> GetSystemResourceSummary()
        {
            try
            {
                _logger.LogInformation("Getting system resource summary");
                var summary = await _systemStatisticsService.GetSystemResourceSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system resource summary");
                return StatusCode(500, new { message = "An error occurred while retrieving system resource summary" });
            }
        }
    }
}
