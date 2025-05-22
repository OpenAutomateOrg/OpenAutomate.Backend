using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.IRepository;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Constants;
using OpenAutomate.API.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IBotAgentService _botAgentService;
        
        // Standardized log message templates
        private static class LogMessages
        {
            public const string NoTenantContext = "Connection attempt without valid tenant context";
            public const string ProcessingConnection = "Processing connection for machine key {MachineKey} in tenant {TenantId}";
            public const string InvalidMachineKey = "Connection attempt with invalid machine key: {MachineKey} for tenant {TenantId}";
            public const string BotAgentConnected = "Bot agent connected: {BotAgentName} ({BotAgentId})";
            public const string BotAgentDisconnected = "Bot agent disconnected: {BotAgentName} ({BotAgentId})";
            public const string BotStatusUpdate = "Bot status update: {BotAgentName} - {Status}";
            public const string CommandSent = "Command sent to bot agent {BotAgentId}: {Command}";
            public const string DisconnectionError = "Error during bot agent disconnection: {Message}";
            public const string QueryNullReference = "HTTP context or query parameter is null during disconnection";
            public const string KeepAliveReceived = "Keep-alive received from bot agent: {BotAgentName} ({BotAgentId})";
            public const string ResolvingTenant = "Resolving tenant from slug: {TenantSlug}";
            public const string TenantResolved = "Tenant resolved successfully for slug: {TenantSlug}";
            public const string TenantResolutionFailed = "Failed to resolve tenant from slug: {TenantSlug}";
        }
        
        public BotAgentHub(
            ITenantContext tenantContext, 
            IUnitOfWork unitOfWork,
            IBotAgentService botAgentService,
            ILogger<BotAgentHub> logger)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _botAgentService = botAgentService ?? throw new ArgumentNullException(nameof(botAgentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Handles connection of a bot agent
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var machineKey = httpContext?.Request.Query["machineKey"].ToString();
            if (string.IsNullOrEmpty(machineKey) && httpContext?.Request.Headers.ContainsKey("X-MachineKey") == true)
            {
                machineKey = httpContext.Request.Headers["X-MachineKey"].ToString();
            }
            if (!_tenantContext.HasTenant)
            {
                var tenantSlug = httpContext?.GetTenantSlug();
                if (!string.IsNullOrEmpty(tenantSlug))
                {
                    _logger.LogInformation(LogMessages.ResolvingTenant, tenantSlug);
                    var tenantResolved = await _botAgentService.ResolveTenantFromSlugAsync(tenantSlug);
                    if (!tenantResolved)
                    {
                        _logger.LogWarning(LogMessages.TenantResolutionFailed, tenantSlug);
                        Context.Abort();
                        return;
                    }
                    _logger.LogInformation(LogMessages.TenantResolved, tenantSlug);
                }
                else
                {
                    _logger.LogWarning(LogMessages.NoTenantContext);
                    Context.Abort();
                    return;
                }
            }
            if (!string.IsNullOrEmpty(machineKey))
            {
                _logger.LogDebug(LogMessages.ProcessingConnection, machineKey, _tenantContext.CurrentTenantId);
                var botAgent = await _botAgentService.ConnectBotAgentAsync(machineKey);
                if (botAgent == null)
                {
                    _logger.LogWarning(LogMessages.InvalidMachineKey, machineKey, _tenantContext.CurrentTenantId);
                    Context.Abort();
                    return;
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, $"bot-{botAgent.Id}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{_tenantContext.CurrentTenantId}");
                await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync("BotStatusUpdate", 
                    new { 
                        BotAgentId = botAgent.Id,
                        BotAgentName = botAgent.Name,
                        Status = botAgent.Status,
                        LastHeartbeat = botAgent.LastHeartbeat,
                        ExecutionId = (string?)null,
                        Timestamp = DateTime.UtcNow
                    });
                _logger.LogInformation(LogMessages.BotAgentConnected, botAgent.Name, botAgent.Id);
            }
            else if (Context.User?.Identity?.IsAuthenticated == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{_tenantContext.CurrentTenantId}");
                _logger.LogInformation("Frontend user connected to tenant group {TenantId}", _tenantContext.CurrentTenantId);
            }
            else
            {
                _logger.LogWarning("Connection attempt without valid machine key or user authentication");
                Context.Abort();
                return;
            }
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
                if (string.IsNullOrEmpty(machineKey) && httpContext.Request.Headers.ContainsKey("X-MachineKey"))
                {
                    machineKey = httpContext.Request.Headers["X-MachineKey"].ToString();
                }
                if (!_tenantContext.HasTenant)
                {
                    var tenantSlug = httpContext.GetTenantSlug();
                    if (!string.IsNullOrEmpty(tenantSlug))
                    {
                        _logger.LogInformation(LogMessages.ResolvingTenant, tenantSlug);
                        var tenantResolved = await _botAgentService.ResolveTenantFromSlugAsync(tenantSlug);
                        if (!tenantResolved)
                        {
                            _logger.LogWarning(LogMessages.TenantResolutionFailed, tenantSlug);
                            await base.OnDisconnectedAsync(exception);
                            return;
                        }
                        _logger.LogInformation(LogMessages.TenantResolved, tenantSlug);
                    }
                    else
                    {
                        _logger.LogWarning(LogMessages.NoTenantContext);
                        await base.OnDisconnectedAsync(exception);
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(machineKey))
                {
                    var botAgent = await _botAgentService.DisconnectBotAgentAsync(machineKey);
                    if (botAgent != null)
                    {
                        await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync("BotStatusUpdate", 
                            new { 
                                BotAgentId = botAgent.Id,
                                BotAgentName = botAgent.Name,
                                Status = botAgent.Status,
                                LastHeartbeat = botAgent.LastHeartbeat,
                                ExecutionId = (string?)null,
                                Timestamp = DateTime.UtcNow
                            });
                        _logger.LogInformation(LogMessages.BotAgentDisconnected, botAgent.Name, botAgent.Id);
                    }
                }
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.DisconnectionError, ex.Message);
                await base.OnDisconnectedAsync(exception);
            }
        }
        
        /// <summary>
        /// Method for bot agent to send status updates
        /// </summary>
        /// <param name="status">Current status of the bot agent</param>
        /// <param name="executionId">Optional execution ID if the status is related to a specific execution</param>
        public async Task SendStatusUpdate(string status, string? executionId = null)
        {
            var httpContext = Context.GetHttpContext();
            var machineKey = httpContext?.Request.Query["machineKey"].ToString();
            if (string.IsNullOrEmpty(machineKey) && httpContext?.Request.Headers.ContainsKey("X-MachineKey") == true)
            {
                machineKey = httpContext.Request.Headers["X-MachineKey"].ToString();
            }
            var botAgent = await _botAgentService.UpdateBotAgentStatusAsync(machineKey, status, executionId);
            if (botAgent == null) return;
            var updateData = new {
                BotAgentId = botAgent.Id,
                BotAgentName = botAgent.Name,
                Status = botAgent.Status,
                ExecutionId = executionId,
                Timestamp = DateTime.UtcNow
            };
            await Clients.Group($"tenant-{_tenantContext.CurrentTenantId}").SendAsync(
                "BotStatusUpdate", updateData);
            string botAgentName = botAgent.Name?.ToString() ?? "Unknown";
            _logger.LogDebug(LogMessages.BotStatusUpdate, botAgentName, status);
        }
        
        /// <summary>
        /// Lightweight method for bot agent to keep the connection alive without sending status updates
        /// </summary>
        public async Task KeepAlive()
        {
            var httpContext = Context.GetHttpContext();
            var machineKey = httpContext?.Request.Query["machineKey"].ToString();
            if (string.IsNullOrEmpty(machineKey) && httpContext?.Request.Headers.ContainsKey("X-MachineKey") == true)
            {
                machineKey = httpContext.Request.Headers["X-MachineKey"].ToString();
            }
            var botAgent = await _botAgentService.KeepAliveAsync(machineKey);
            if (botAgent == null) return;
            string botAgentName = botAgent.Name?.ToString() ?? "Unknown";
            _logger.LogTrace(LogMessages.KeepAliveReceived, botAgentName, botAgent.Id);
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
            if (string.IsNullOrEmpty(machineKey) && httpContext.Request.Headers.ContainsKey("X-MachineKey"))
            {
                machineKey = httpContext.Request.Headers["X-MachineKey"].ToString();
            }
            if (!_tenantContext.HasTenant)
            {
                var tenantSlug = httpContext.GetTenantSlug();
                if (!string.IsNullOrEmpty(tenantSlug))
                {
                    var tenantResolved = await _botAgentService.ResolveTenantFromSlugAsync(tenantSlug);
                    if (!tenantResolved)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            if (string.IsNullOrEmpty(machineKey))
                return null;
            return await _botAgentService.UpdateBotAgentStatusAsync(machineKey, null);
        }
    }
} 