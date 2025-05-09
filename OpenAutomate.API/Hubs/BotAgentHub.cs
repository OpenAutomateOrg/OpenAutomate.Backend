using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.API.Hubs
{
    /// <summary>
    /// SignalR hub for real-time communication with bot agents
    /// </summary>
    public class BotAgentHub : Hub
    {
        private readonly ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BotAgentHub> _logger;
        
        // Standardized log message templates
        private static class LogMessages
        {
            public const string MissingMachineKey = "Connection attempt with missing machine key";
            public const string NoTenantContext = "Connection attempt without valid tenant context";
            public const string ProcessingConnection = "Processing connection for machine key {MachineKey} in tenant {TenantId}";
            public const string InvalidMachineKey = "Connection attempt with invalid machine key: {MachineKey} for tenant {TenantId}";
            public const string BotAgentConnected = "Bot agent connected: {BotAgentName} ({BotAgentId})";
            public const string BotAgentDisconnected = "Bot agent disconnected: {BotAgentName} ({BotAgentId})";
            public const string BotStatusUpdate = "Bot status update: {BotAgentName} - {Status}";
            public const string CommandSent = "Command sent to bot agent {BotAgentId}: {Command}";
            public const string DisconnectionError = "Error during bot agent disconnection: {Message}";
            public const string QueryNullReference = "HTTP context or query parameter is null during disconnection";
        }
        
        public BotAgentHub(
            ITenantContext tenantContext, 
            IUnitOfWork unitOfWork,
            ILogger<BotAgentHub> logger)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Handles connection of a bot agent
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            
            // Extract machine key
            var machineKey = httpContext?.Request.Query["machineKey"].ToString() ?? string.Empty;
            
            if (string.IsNullOrEmpty(machineKey))
            {
                _logger.LogWarning(LogMessages.MissingMachineKey);
                Context.Abort();
                return;
            }
            
            // Tenant context is already set by the TenantResolutionMiddleware
            if (!_tenantContext.HasTenant)
            {
                _logger.LogWarning(LogMessages.NoTenantContext);
                Context.Abort();
                return;
            }
            
            _logger.LogDebug(LogMessages.ProcessingConnection, machineKey, _tenantContext.CurrentTenantId);
            
            // Verify bot agent exists and belongs to this tenant
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            if (botAgent == null)
            {
                _logger.LogWarning(LogMessages.InvalidMachineKey, machineKey, _tenantContext.CurrentTenantId);
                Context.Abort();
                return;
            }
            
            // Add to bot-specific group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"bot-{botAgent.Id}");
            
            // Add to tenant group for tenant-wide broadcasts
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{_tenantContext.CurrentTenantId}");
            
            // Update bot agent status
            botAgent.Status = "Online";
            botAgent.LastHeartbeat = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            
            // Notify frontend clients about bot agent status change
            await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync("BotStatusChanged", 
                new { 
                    Id = botAgent.Id,
                    Name = botAgent.Name,
                    Status = botAgent.Status,
                    LastHeartbeat = botAgent.LastHeartbeat
                });
                
            _logger.LogInformation(LogMessages.BotAgentConnected, botAgent.Name, botAgent.Id);
            
            await base.OnConnectedAsync();
        }
        
        /// <summary>
        /// Handles disconnection of a bot agent
        /// </summary>
        /// <param name="exception">Optional exception that caused the disconnection</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext == null)
                {
                    _logger.LogWarning(LogMessages.QueryNullReference);
                    return;
                }
                
                var machineKey = httpContext.Request.Query["machineKey"].ToString();
                
                if (!string.IsNullOrEmpty(machineKey))
                {
                    // Use the tenant context that was set by middleware
                    var botAgent = await _unitOfWork.BotAgents
                        .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
                        
                    if (botAgent != null)
                    {
                        botAgent.Status = "Offline";
                        await _unitOfWork.CompleteAsync();
                        
                        // Notify frontend clients about bot agent status change
                        await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync("BotStatusChanged", 
                            new { 
                                Id = botAgent.Id,
                                Name = botAgent.Name,
                                Status = botAgent.Status,
                                LastHeartbeat = botAgent.LastHeartbeat
                            });
                            
                        _logger.LogInformation(LogMessages.BotAgentDisconnected, botAgent.Name, botAgent.Id);
                        
                        // Log the exception if present
                        if (exception != null)
                        {
                            _logger.LogWarning(exception, "Bot agent disconnected with exception: {BotAgentName} ({BotAgentId})", 
                                botAgent.Name, botAgent.Id);
                        }
                    }
                }
                
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.DisconnectionError, ex.Message);
                // Still call the base method to ensure proper cleanup
                await base.OnDisconnectedAsync(exception);
            }
        }
        
        /// <summary>
        /// Method for bot agent to send status updates
        /// </summary>
        /// <param name="status">Current status of the bot agent</param>
        /// <param name="executionId">Optional execution ID if the status is related to a specific execution</param>
        public async Task SendStatusUpdate(string status, string executionId = null)
        {
            var botAgent = await GetBotAgentFromContext();
            if (botAgent == null) return;
            
            botAgent.LastHeartbeat = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            
            var updateData = new {
                BotAgentId = botAgent.Id,
                BotAgentName = botAgent.Name,
                Status = status,
                ExecutionId = executionId,
                Timestamp = DateTime.UtcNow
            };
            
            // Notify frontend about status update
            await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync(
                "BotStatusUpdate", updateData);
                
            // Cast dynamic botAgent to avoid extension method issues
            string botAgentName = botAgent.Name?.ToString() ?? "Unknown";
            _logger.LogDebug(LogMessages.BotStatusUpdate, botAgentName, status);
        }
        
        /// <summary>
        /// Method for server to send commands to bot agents
        /// </summary>
        /// <param name="botAgentId">ID of the target bot agent</param>
        /// <param name="command">Command to execute</param>
        /// <param name="payload">Additional data for the command</param>
        public async Task SendCommandToBotAgent(string botAgentId, string command, object payload)
        {
            await Clients.Group($"bot-{botAgentId}").SendAsync("ReceiveCommand", command, payload);
            _logger.LogInformation(LogMessages.CommandSent, botAgentId, command);
        }
        
        /// <summary>
        /// Helper method to get the bot agent associated with the current connection
        /// </summary>
        /// <returns>The bot agent or null if not found</returns>
        private async Task<BotAgent?> GetBotAgentFromContext()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                _logger.LogWarning(LogMessages.QueryNullReference);
                return null;
            }
            
            var machineKey = httpContext.Request.Query["machineKey"].ToString();
            
            if (string.IsNullOrEmpty(machineKey))
                return null;
                
            return await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
        }
    }
} 