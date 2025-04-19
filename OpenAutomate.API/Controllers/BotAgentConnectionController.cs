using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Bot Agent connection and status updates
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/agent")]
    public class BotAgentConnectionController : ControllerBase
    {
        private readonly IBotAgentService _botAgentService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentConnectionController"/> class
        /// </summary>
        /// <param name="botAgentService">The Bot Agent service</param>
        public BotAgentConnectionController(IBotAgentService botAgentService)
        {
            _botAgentService = botAgentService;
        }
        
        /// <summary>
        /// Connects or updates a Bot Agent with the server
        /// </summary>
        /// <param name="request">The connection request containing machine key</param>
        /// <param name="tenant">The tenant slug from the route</param>
        /// <returns>Bot Agent information if successfully connected</returns>
        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] BotAgentConnectionRequest request, [FromRoute] string tenant)
        {
            try
            {
                var result = await _botAgentService.ValidateAndConnectBotAgentAsync(request, tenant);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid machine key" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        /// <summary>
        /// Updates Bot Agent status
        /// </summary>
        /// <param name="request">The status update request</param>
        /// <param name="tenant">The tenant slug from the route</param>
        /// <returns>Success or error response</returns>
        [HttpPost("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] BotAgentStatusUpdateRequest request, [FromRoute] string tenant)
        {
            try
            {
                await _botAgentService.UpdateBotAgentStatusAsync(request, tenant);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid machine key" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        /// <summary>
        /// Gets available assets for Bot Agent
        /// </summary>
        /// <param name="machineKey">The Bot Agent's machine key</param>
        /// <param name="tenant">The tenant slug from the route</param>
        /// <returns>Collection of assets available to the Bot Agent</returns>
        [HttpGet("assets")]
        public async Task<IActionResult> GetAvailableAssets([FromQuery] string machineKey, [FromRoute] string tenant)
        {
            try
            {
                var assets = await _botAgentService.GetAssetsForBotAgentAsync(machineKey, tenant);
                return Ok(assets);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid machine key" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 