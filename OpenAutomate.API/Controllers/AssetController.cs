using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Asset management operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/assets")]
    [Authorize]
    public class AssetController : CustomControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetController> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetController"/> class
        /// </summary>
        /// <param name="assetService">The Asset service</param>
        /// <param name="logger">The logger</param>
        public AssetController(
            IAssetService assetService, 
            ILogger<AssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }
        
        /// <summary>
        /// Creates a new Asset
        /// </summary>
        /// <param name="dto">The Asset creation data (Key, Value, etc.)</param>
        /// <returns>The created Asset</returns>
        [HttpPost]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AssetResponseDto>> CreateAsset([FromBody] CreateAssetDto dto)
        {
            try
            {
                var asset = await _assetService.CreateAssetAsync(dto);
                
                // Get the tenant from the route data
                var tenant = RouteData.Values["tenant"]?.ToString();
                
                return CreatedAtAction(
                    nameof(GetAssetById),
                    new { tenant = tenant, id = asset.Id },
                    asset);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating asset: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while creating the asset." });
            }
        }
        
        /// <summary>
        /// Gets all Assets for the current tenant
        /// </summary>
        /// <returns>Collection of Assets</returns>
        [HttpGet]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AssetListResponseDto>>> GetAllAssets()
        {
            try
            {
                var assets = await _assetService.GetAllAssetsAsync();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all assets: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving assets." });
            }
        }
        
        /// <summary>
        /// Gets an Asset by its ID
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <returns>The Asset if found</returns>
        [HttpGet("{id}")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponseDto>> GetAssetById(Guid id)
        {
            try
            {
                var asset = await _assetService.GetAssetByIdAsync(id);
                if (asset == null)
                    return NotFound(new { message = $"Asset with ID {id} not found." });
                    
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving the asset." });
            }
        }
        
        /// <summary>
        /// Gets an Asset by its key
        /// </summary>
        /// <param name="key">The Asset key</param>
        /// <returns>The Asset if found</returns>
        [HttpGet("key/{key}")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponseDto>> GetAssetByKey(string key)
        {
            try
            {
                var asset = await _assetService.GetAssetByKeyAsync(key);
                if (asset == null)
                    return NotFound(new { message = $"Asset with key '{key}' not found." });
                    
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by key {Key}: {Message}", key, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving the asset." });
            }
        }
        
        /// <summary>
        /// Updates an existing Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <param name="dto">The updated Asset data</param>
        /// <returns>The updated Asset</returns>
        [HttpPut("{id}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssetResponseDto>> UpdateAsset(Guid id, [FromBody] UpdateAssetDto dto)
        {
            try
            {
                var asset = await _assetService.UpdateAssetAsync(id, dto);
                
                return Ok(asset);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset with ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating the asset." });
            }
        }
        
        /// <summary>
        /// Deletes an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        [HttpDelete("{id}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsset(Guid id)
        {
            try
            {
                var deleted = await _assetService.DeleteAssetAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Asset with ID {id} not found." });
                    
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting asset with ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while deleting the asset." });
            }
        }
        
        /// <summary>
        /// Gets all Bot Agents authorized to access an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <returns>Collection of authorized Bot Agents</returns>
        [HttpGet("{id}/bot-agents")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<BotAgentSummaryDto>>> GetAuthorizedBotAgents(Guid id)
        {
            try
            {
                var botAgents = await _assetService.GetAuthorizedBotAgentsAsync(id);
                return Ok(botAgents);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorized bot agents for asset {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving authorized bot agents." });
            }
        }
        
        /// <summary>
        /// Updates the Bot Agents authorized to access an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <param name="dto">The list of Bot Agent IDs</param>
        [HttpPut("{id}/bot-agents")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAuthorizedBotAgents(Guid id, [FromBody] AssetBotAgentDto dto)
        {
            try
            {
                var updated = await _assetService.UpdateAuthorizedBotAgentsAsync(id, dto);
                return Ok(new { success = updated });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating authorized bot agents for asset {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating authorized bot agents." });
            }
        }
        
        /// <summary>
        /// Authorizes a Bot Agent to access an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <param name="botAgentId">The Bot Agent ID</param>
        [HttpPost("{id}/bot-agents/{botAgentId}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AuthorizeBotAgent(Guid id, Guid botAgentId)
        {
            try
            {
                var authorized = await _assetService.AuthorizeBotAgentAsync(id, botAgentId);
                return Ok(new { success = authorized });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authorizing bot agent {BotAgentId} for asset {Id}: {Message}", 
                    botAgentId, id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while authorizing the bot agent." });
            }
        }
        
        /// <summary>
        /// Revokes a Bot Agent's access to an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <param name="botAgentId">The Bot Agent ID</param>
        [HttpDelete("{id}/bot-agents/{botAgentId}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Update)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeBotAgent(Guid id, Guid botAgentId)
        {
            try
            {
                var revoked = await _assetService.RevokeBotAgentAsync(id, botAgentId);
                if (!revoked)
                    return NotFound(new { message = "Asset or bot agent not found, or no relationship exists." });
                    
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking bot agent {BotAgentId} for asset {Id}: {Message}", 
                    botAgentId, id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while revoking the bot agent." });
            }
        }
        
        /// <summary>
        /// Exports all Assets to CSV format
        /// </summary>
        /// <param name="includeSecrets">Whether to include actual secret values or use placeholders (default: false for security)</param>
        /// <returns>CSV file download</returns>
        [HttpGet("export/csv")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportAssetsToCsv([FromQuery] bool includeSecrets = false)
        {
            try
            {
                var csvData = await _assetService.ExportAssetsToCsvAsync(includeSecrets);
                var fileName = $"assets_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                return File(csvData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting assets to CSV: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while exporting assets." });
            }
        }
        
        /// <summary>
        /// Imports Assets from CSV file
        /// </summary>
        /// <param name="file">CSV file containing asset data</param>
        /// <returns>Import result with statistics and errors</returns>
        [HttpPost("import/csv")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.AssetResource, Permissions.Create)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CsvImportResultDto>> ImportAssetsFromCsv(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Please select a CSV file to import." });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Only CSV files are allowed." });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return BadRequest(new { message = "File size must be less than 10MB." });
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var csvData = memoryStream.ToArray();

                var result = await _assetService.ImportAssetsFromCsvAsync(csvData);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing assets from CSV: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while importing assets." });
            }
        }
    }
} 