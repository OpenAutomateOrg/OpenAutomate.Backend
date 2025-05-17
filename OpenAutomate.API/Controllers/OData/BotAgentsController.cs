using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Constants;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Bot Agents
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Bot Agents
    /// </remarks>
    [Route("{tenant}/odata/BotAgents")]
    [ApiController]
    [Authorize]
    public class BotAgentsController : ODataController
    {
        private readonly IBotAgentService _botAgentService;
        private readonly ILogger<BotAgentsController> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentsController"/> class
        /// </summary>
        /// <param name="botAgentService">The Bot Agent service</param>
        /// <param name="logger">The logger</param>
        public BotAgentsController(IBotAgentService botAgentService, ILogger<BotAgentsController> logger)
        {
            _botAgentService = botAgentService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets all Bot Agents with OData query support
        /// </summary>
        /// <returns>Collection of Bot Agents that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/BotAgents?$filter=Status eq 'Active'
        /// GET /tenant/odata/BotAgents?$orderby=Name asc&$top=10&$skip=10
        /// GET /tenant/odata/BotAgents?$select=Id,Name,Status
        /// </remarks>
        [HttpGet]
        [RequirePermission(Resources.AgentResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                var botAgents = await _botAgentService.GetAllBotAgentsAsync();
                return Ok(botAgents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot agents for OData query");
                return StatusCode(500, "An error occurred while retrieving bot agents");
            }
        }
        
        /// <summary>
        /// Gets a specific Bot Agent by ID with OData query support
        /// </summary>
        /// <param name="key">The Bot Agent ID</param>
        /// <returns>The Bot Agent if found</returns>
        /// <remarks>
        /// Example query:
        /// GET /tenant/odata/BotAgents(guid'12345678-1234-1234-1234-123456789012')?$select=Name,Status
        /// </remarks>
        [HttpGet("{key}")]
        [RequirePermission(Resources.AgentResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var botAgent = await _botAgentService.GetBotAgentByIdAsync(key);
                if (botAgent == null)
                    return NotFound();
                    
                return Ok(botAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot agent with ID {BotAgentId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the bot agent");
            }
        }
    }
} 