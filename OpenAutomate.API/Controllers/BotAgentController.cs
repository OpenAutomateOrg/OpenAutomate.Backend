using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Constants;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Bot Agent management operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/agents")]
    [Authorize]
    public class BotAgentController : ControllerBase
    {
        private readonly IBotAgentService _botAgentService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentController"/> class
        /// </summary>
        /// <param name="botAgentService">The Bot Agent service</param>
        public BotAgentController(IBotAgentService botAgentService)
        {
            _botAgentService = botAgentService;
        }
        
        /// <summary>
        /// Creates a new Bot Agent and generates a machine key
        /// </summary>
        /// <param name="dto">The Bot Agent creation data</param>
        /// <returns>The CreatedAtBot Agent with machine key</returns>
        [HttpPost("create")]
        [RequirePermission(Resources.AgentResource, Permissions.Create)]
        public async Task<ActionResult<BotAgentResponseDto>> CreateBotAgent([FromBody] CreateBotAgentDto dto)
        {
            var botAgent = await _botAgentService.CreateBotAgentAsync(dto);
            
            // Get the tenant from the route data
            var tenant = RouteData.Values["tenant"]?.ToString();
            
            return CreatedAtAction(
                nameof(GetBotAgentById), 
                new { tenant = tenant, id = botAgent.Id }, 
                botAgent);
        }
        
        /// <summary>
        /// Gets a Bot Agent by its ID
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <returns>The Bot Agent if found</returns>
        [HttpGet("{id}")]
        [RequirePermission(Resources.AgentResource, Permissions.View)]
        public async Task<ActionResult<BotAgentResponseDto>> GetBotAgentById(Guid id)
        {
            var botAgent = await _botAgentService.GetBotAgentByIdAsync(id);
            if (botAgent == null)
                return NotFound();
                
            return Ok(botAgent);
        }
        
        /// <summary>
        /// Gets all Bot Agents for the current tenant
        /// </summary>
        /// <returns>Collection of Bot Agents</returns>
        [HttpGet]
        [RequirePermission(Resources.AgentResource, Permissions.View)]
        public async Task<ActionResult<IEnumerable<BotAgentResponseDto>>> GetAllBotAgents()
        {
            var botAgents = await _botAgentService.GetAllBotAgentsAsync();
            return Ok(botAgents);
        }
        
        /// <summary>
        /// Regenerates the machine key for a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <returns>The updated Bot Agent with new machine key</returns>
        [HttpPost("{id}/regenerateKey")]
        [RequirePermission(Resources.AgentResource, Permissions.Update)]
        public async Task<ActionResult<BotAgentResponseDto>> RegenerateMachineKey(Guid id)
        {
            var botAgent = await _botAgentService.RegenerateMachineKeyAsync(id);
            return Ok(botAgent);
        }
        
        /// <summary>
        /// Deactivates a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        [HttpPost("{id}/deactivate")]
        [RequirePermission(Resources.AgentResource, Permissions.Update)]
        public async Task<IActionResult> DeactivateBotAgent(Guid id)
        {
            await _botAgentService.DeactivateBotAgentAsync(id);
            return NoContent();
        }
    }
} 