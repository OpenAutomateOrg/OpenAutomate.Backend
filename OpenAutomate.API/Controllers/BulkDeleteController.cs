using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for bulk delete operations on individual entity types
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/bulk-delete")]
    [Authorize]
    public class BulkDeleteController : CustomControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly IBotAgentService _botAgentService;
        private readonly IScheduleService _scheduleService;
        private readonly IAutomationPackageService _automationPackageService;
        private readonly IOrganizationUnitUserService _organizationUnitUserService;
        private readonly ILogger<BulkDeleteController> _logger;

        public BulkDeleteController(
            IAssetService assetService,
            IBotAgentService botAgentService,
            IScheduleService scheduleService,
            IAutomationPackageService automationPackageService,
            IOrganizationUnitUserService organizationUnitUserService,
            ILogger<BulkDeleteController> logger)
        {
            _assetService = assetService;
            _botAgentService = botAgentService;
            _scheduleService = scheduleService;
            _automationPackageService = automationPackageService;
            _organizationUnitUserService = organizationUnitUserService;
            _logger = logger;
        }

        /// <summary>
        /// Bulk delete assets
        /// </summary>
        /// <param name="dto">Bulk delete request with asset IDs</param>
        [HttpDelete("assets")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Delete)]
        [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BulkDeleteResultDto>> BulkDeleteAssets([FromBody] BulkDeleteDto dto)
        {
            try
            {
                var result = await _assetService.BulkDeleteAssetsAsync(dto.Ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting assets: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while bulk deleting assets." });
            }
        }

        /// <summary>
        /// Bulk delete bot agents
        /// </summary>
        /// <param name="dto">Bulk delete request with bot agent IDs</param>
        [HttpDelete("agents")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AgentResource, Permissions.Delete)]
        [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BulkDeleteResultDto>> BulkDeleteBotAgents([FromBody] BulkDeleteDto dto)
        {
            try
            {
                var result = await _botAgentService.BulkDeleteBotAgentsAsync(dto.Ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting bot agents: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while bulk deleting bot agents." });
            }
        }

        /// <summary>
        /// Bulk delete schedules
        /// </summary>
        /// <param name="dto">Bulk delete request with schedule IDs</param>
        [HttpDelete("schedules")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.ScheduleResource, Permissions.Delete)]
        [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BulkDeleteResultDto>> BulkDeleteSchedules([FromBody] BulkDeleteDto dto)
        {
            try
            {
                var result = await _scheduleService.BulkDeleteSchedulesAsync(dto.Ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting schedules: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while bulk deleting schedules." });
            }
        }

        /// <summary>
        /// Bulk delete automation packages
        /// </summary>
        /// <param name="dto">Bulk delete request with package IDs</param>
        [HttpDelete("packages")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Delete)]
        [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BulkDeleteResultDto>> BulkDeletePackages([FromBody] BulkDeleteDto dto)
        {
            try
            {
                var result = await _automationPackageService.BulkDeletePackagesAsync(dto.Ids);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting packages: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while bulk deleting packages." });
            }
        }

        /// <summary>
        /// Bulk remove users from organization unit
        /// </summary>
        /// <param name="tenant">The tenant slug (organization unit)</param>
        /// <param name="dto">Bulk remove request with user IDs</param>
        [HttpDelete("remove-users")]
        [RequirePermission(Resources.UserResource, Permissions.Delete)]
        [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BulkDeleteResultDto>> BulkRemoveUsersFromOU([FromRoute] string tenant, [FromBody] BulkDeleteDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _organizationUnitUserService.BulkRemoveUsersAsync(tenant, dto.Ids, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk removing users from organization unit: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while bulk removing users from organization unit." });
            }
        }

    }
}
