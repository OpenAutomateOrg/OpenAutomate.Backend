using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Bot Agent asset retrieval operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/bot-agent/assets")]
    [AllowAnonymous] // Bot agents authenticate via machine key rather than JWT
    public class BotAgentAssetController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<BotAgentAssetController> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentAssetController"/> class
        /// </summary>
        /// <param name="assetService">The Asset service</param>
        /// <param name="logger">The logger</param>
        public BotAgentAssetController(IAssetService assetService, ILogger<BotAgentAssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets an Asset value by its key using machine key authentication
        /// </summary>
        /// <param name="key">The Asset key</param>
        /// <param name="request">The request containing the machine key</param>
        /// <returns>The Asset value if found and bot agent is authorized</returns>
        [HttpPost("key/{key}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssetValueByKey(string key, [FromBody] BotAgentAssetDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.MachineKey))
                {
                    _logger.LogWarning("Bot agent attempted to access asset with missing machine key");
                    return Unauthorized(new { message = "Machine key is required" });
                }
                
                var assetValue = await _assetService.GetAssetValueForBotAgentAsync(key, request.MachineKey);
                if (assetValue == null)
                {
                    return NotFound(new { message = $"Asset with key '{key}' not found or bot agent not authorized" });
                }
                
                return Ok(assetValue);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized bot agent attempted to access asset '{Key}'", key);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset value by key '{Key}': {Message}", key, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving the asset value." });
            }
        }
        
        /// <summary>
        /// Gets all Assets accessible by a bot agent using machine key authentication
        /// </summary>
        /// <param name="request">The request containing the machine key</param>
        /// <returns>Collection of accessible Assets</returns>
        [HttpPost("accessible")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccessibleAssets([FromBody] BotAgentKeyDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.MachineKey))
                {
                    _logger.LogWarning("Bot agent attempted to list accessible assets with missing machine key");
                    return Unauthorized(new { message = "Machine key is required" });
                }
                
                var assets = await _assetService.GetAccessibleAssetsForBotAgentAsync(request.MachineKey);
                if (assets == null)
                {
                    return NotFound(new { message = "Bot agent not found" });
                }
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible assets: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving accessible assets." });
            }
        }
    }
} 