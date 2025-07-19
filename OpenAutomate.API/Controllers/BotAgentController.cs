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
    /// Controller for Bot Agent CRUD management operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/agents")]
    [Authorize]
    public class BotAgentController : ControllerBase
    {
        private readonly IBotAgentService _botAgentService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ITenantContext _tenantContext;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentController"/> class
        /// </summary>
        /// <param name="botAgentService">The Bot Agent service</param>
        /// <param name="cacheInvalidationService">The cache invalidation service</param>
        /// <param name="tenantContext">The tenant context</param>
        public BotAgentController(
            IBotAgentService botAgentService,
            ICacheInvalidationService cacheInvalidationService,
            ITenantContext tenantContext)
        {
            _botAgentService = botAgentService;
            _cacheInvalidationService = cacheInvalidationService;
            _tenantContext = tenantContext;
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
            
            // Invalidate bot agents cache
            if (_tenantContext.HasTenant)
            {
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/BotAgents", _tenantContext.CurrentTenantId);
            }
            
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
            
            // Invalidate bot agents cache
            if (_tenantContext.HasTenant)
            {
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/BotAgents", _tenantContext.CurrentTenantId);
            }
            
            return NoContent();
        }

        /// <summary>
        /// Deletes a Bot Agent.
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        [HttpDelete("{id}")]
        [RequirePermission(Resources.AgentResource, Permissions.Delete)]
        public async Task<IActionResult> DeleteBotAgent(Guid id)
        {
            try
            {
                await _botAgentService.DeleteBotAgentAsync(id);
                
                // Invalidate bot agents cache
                if (_tenantContext.HasTenant)
                {
                    await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/BotAgents", _tenantContext.CurrentTenantId);
                }
                
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ApplicationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a Bot Agent's editable fields
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <param name="dto">The update data</param>
        /// <returns>The updated Bot Agent</returns>
        [HttpPut("{id}")]
        [RequirePermission(Resources.AgentResource, Permissions.Update)]
        public async Task<ActionResult<BotAgentResponseDto>> UpdateBotAgent(Guid id, [FromBody] UpdateBotAgentDto dto)
        {
            var updatedAgent = await _botAgentService.UpdateBotAgentAsync(id, dto);
            
            // Invalidate bot agents cache
            if (_tenantContext.HasTenant)
            {
                await _cacheInvalidationService.InvalidateApiResponseCacheAsync("/odata/BotAgents", _tenantContext.CurrentTenantId);
            }
            
            return Ok(updatedAgent);
        }
    }
} 