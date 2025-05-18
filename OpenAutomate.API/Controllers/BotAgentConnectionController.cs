using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Constants;
using Microsoft.AspNetCore.SignalR;
using OpenAutomate.API.Hubs;
using OpenAutomate.API.Extensions;
using Microsoft.Extensions.Logging;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Bot Agent real-time connection and command operations
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/agent-connection")]
    public class BotAgentConnectionController : ControllerBase
    {
        private readonly IBotAgentService _botAgentService;
        private readonly IHubContext<BotAgentHub> _hubContext;
        private readonly ILogger<BotAgentConnectionController> _logger;
        private readonly ITenantContext _tenantContext;
        
        // Standardized log message templates
        private static class LogMessages
        {
            public const string NoTenantContext = "Attempt to connect bot agent without valid tenant context";
            public const string InvalidMachineKey = "Connection attempt with invalid machine key: {MachineKey} for tenant {TenantId}";
            public const string AgentConnected = "Bot agent connected: {MachineName} with key {MachineKey} for tenant {TenantId}";
            public const string NoTenantContextForCommand = "Attempt to send command without valid tenant context";
            public const string NonExistentBotAgent = "Attempt to send command to non-existent bot agent: {BotAgentId}";
            public const string OfflineBotAgent = "Attempt to send command to offline bot agent: {BotAgentId}";
            public const string CommandSent = "Command {CommandType} sent to bot agent {BotAgentId} for tenant {TenantId}";
            public const string CommandError = "Error sending command to bot agent {BotAgentId}";
            public const string NoTenantContextForStatus = "Attempt to get bot agent status without valid tenant context";
            public const string StatusError = "Error getting status for bot agent {BotAgentId}";
            public const string NoTenantContextForBroadcast = "Attempt to broadcast notification without valid tenant context";
            public const string NotificationBroadcast = "Notification '{NotificationType}' broadcast to tenant {TenantId}";
            public const string BroadcastError = "Error broadcasting notification";
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentConnectionController"/> class
        /// </summary>
        /// <param name="botAgentService">The Bot Agent service</param>
        /// <param name="hubContext">The SignalR hub context</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="tenantContext">The tenant context</param>
        public BotAgentConnectionController(
            IBotAgentService botAgentService,
            IHubContext<BotAgentHub> hubContext,
            ILogger<BotAgentConnectionController> logger,
            ITenantContext tenantContext)
        {
            _botAgentService = botAgentService ?? throw new ArgumentNullException(nameof(botAgentService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }
        
        /// <summary>
        /// Authenticates a Bot Agent for real-time communication
        /// </summary>
        /// <param name="connectionRequest">The connection request with machine key</param>
        /// <returns>Connection details for the SignalR hub</returns>
        [HttpPost("connect")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Bot agents can connect without JWT
        public async Task<IActionResult> ConnectBotAgent([FromBody] BotAgentConnectionRequest connectionRequest)
        {
            try
            {
                // Validate input
                if (connectionRequest == null)
                {
                    return BadRequest(new { error = "Connection request is required" });
                }
                
                if (string.IsNullOrEmpty(connectionRequest.MachineKey))
                {
                    return BadRequest(new { error = "Machine key is required" });
                }
                
                // Ensure tenant context is properly set
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning(LogMessages.NoTenantContext);
                    return BadRequest(new { error = "Invalid tenant context" });
                }
                
                // Verify the machine key is valid
                var botAgents = await _botAgentService.GetAllBotAgentsAsync();
                var botAgent = botAgents.FirstOrDefault(ba => ba.MachineKey == connectionRequest.MachineKey);
                
                if (botAgent == null)
                {
                    _logger.LogWarning(LogMessages.InvalidMachineKey, 
                        connectionRequest.MachineKey, _tenantContext.CurrentTenantId);
                    return Unauthorized(new { error = "Invalid machine key for this tenant" });
                }
                
                // Get the tenant from the route data
                var tenant = RouteData.Values["tenant"]?.ToString();
                
                _logger.LogInformation(LogMessages.AgentConnected, 
                    connectionRequest.MachineName, connectionRequest.MachineKey, _tenantContext.CurrentTenantId);
                
                // Return the connection details
                return Ok(new
                {
                    hubUrl = $"/{tenant}/hubs/botagent",
                    connectionParams = new
                    {
                        machineKey = connectionRequest.MachineKey
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting bot agent");
                return StatusCode(500, new { error = "Error connecting bot agent", message = ex.Message });
            }
        }

        /// <summary>
        /// Sends a command to a Bot Agent via SignalR
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <param name="command">The command data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/command")]
        [Authorize]
        [RequirePermission(Resources.AgentResource, Permissions.Update)]
        public async Task<IActionResult> SendCommandToBotAgent(Guid id, [FromBody] BotAgentCommandDto command)
        {
            try
            {
                // Validate input
                if (command == null)
                {
                    return BadRequest(new { error = "Command data is required" });
                }
                
                if (string.IsNullOrEmpty(command.CommandType))
                {
                    return BadRequest(new { error = "Command type is required" });
                }
                
                // Ensure tenant context is properly set
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning(LogMessages.NoTenantContextForCommand);
                    return BadRequest(new { error = "Invalid tenant context" });
                }
                
                // Check if bot agent exists
                var botAgent = await _botAgentService.GetBotAgentByIdAsync(id);
                if (botAgent == null)
                {
                    _logger.LogWarning(LogMessages.NonExistentBotAgent, id);
                    return NotFound(new { error = "Bot agent not found" });
                }
                
                // EF Core global filters ensure the bot agent belongs to the current tenant
                
                if (botAgent.Status != "Available" && botAgent.Status != "Busy")
                {
                    _logger.LogWarning(LogMessages.OfflineBotAgent, id);
                    return BadRequest(new { error = "Bot agent is not online" });
                }
                
                // Ensure payload is not null before passing it to the method
                object payload = command.Payload ?? new {}; // Use empty object if payload is null
                
                // Send command via SignalR
                await _botAgentService.SendCommandToBotAgentAsync(
                    _hubContext,
                    id,
                    command.CommandType,
                    payload, // Pass non-null payload
                    _logger);
                
                _logger.LogInformation(LogMessages.CommandSent, 
                    command.CommandType, id, _tenantContext.CurrentTenantId);
                    
                return Ok(new { success = true, message = $"Command {command.CommandType} sent to bot agent {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.CommandError, id);
                return StatusCode(500, new { error = "Error sending command", message = ex.Message });
            }
        }
        
        /// <summary>
        /// Gets the connection status of a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <returns>The connection status</returns>
        [HttpGet("{id}/status")]
        [Authorize]
        [RequirePermission(Resources.AgentResource, Permissions.View)]
        public async Task<IActionResult> GetBotAgentStatus(Guid id)
        {
            try
            {
                // Ensure tenant context is properly set
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning(LogMessages.NoTenantContextForStatus);
                    return BadRequest(new { error = "Invalid tenant context" });
                }
                
                var botAgent = await _botAgentService.GetBotAgentByIdAsync(id);
                if (botAgent == null)
                    return NotFound(new { error = "Bot agent not found" });
                
                // EF Core global filters ensure the bot agent belongs to the current tenant
                
                return Ok(new
                {
                    id = botAgent.Id,
                    name = botAgent.Name,
                    status = botAgent.Status,
                    lastConnected = botAgent.LastConnected,
                    tenantId = _tenantContext.CurrentTenantId // Get tenant ID from context
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.StatusError, id);
                return StatusCode(500, new { error = "Error getting bot agent status", message = ex.Message });
            }
        }

        /// <summary>
        /// Broadcasts a notification to all connected clients in the current tenant
        /// </summary>
        /// <param name="notification">The notification data</param>
        /// <returns>Success status</returns>
        [HttpPost("broadcast")]
        [Authorize]
        [RequirePermission(Resources.AgentResource, Permissions.Update)]
        public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDto notification)
        {
            try
            {
                // Validate input
                if (notification == null)
                {
                    return BadRequest(new { error = "Notification data is required" });
                }
                
                // Ensure tenant context is properly set
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning(LogMessages.NoTenantContextForBroadcast);
                    return BadRequest(new { error = "Invalid tenant context" });
                }
                
                if (string.IsNullOrEmpty(notification.NotificationType))
                {
                    return BadRequest(new { error = "Notification type is required" });
                }
                
                // Ensure data is not null before passing it to the method
                object data = notification.Data ?? new {}; // Use empty object if data is null
                
                // Send notification to all clients in the tenant
                await _botAgentService.SendTenantNotificationAsync(
                    _hubContext,
                    _tenantContext.CurrentTenantId,
                    notification.NotificationType,
                    data, // Pass non-null data
                    _tenantContext,
                    _logger);
                
                _logger.LogInformation(LogMessages.NotificationBroadcast, 
                    notification.NotificationType, _tenantContext.CurrentTenantId);
                
                return Ok(new { success = true, message = $"Notification broadcast to tenant" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.BroadcastError);
                return StatusCode(500, new { error = "Error broadcasting notification", message = ex.Message });
            }
        }
    }
} 