using Microsoft.AspNetCore.SignalR;
using OpenAutomate.API.Hubs;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenAutomate.API.Extensions
{
    /// <summary>
    /// Extension methods for the BotAgentService to interact with SignalR
    /// </summary>
    public static class BotAgentServiceExtensions
    {
        // Standardized log message templates
        private static class LogMessages
        {
            public const string CommandSent = "Command sent to bot agent {BotAgentId}: {Command}";
            public const string CrossTenantNotification = "Attempted cross-tenant notification: Current tenant {CurrentTenantId}, requested tenant {RequestedTenantId}";
            public const string SendingNotification = "Sending notification to tenant {TenantId}: {NotificationType}";
        }

        /// <summary>
        /// Sends a command to a bot agent via SignalR
        /// </summary>
        /// <param name="botAgentService">The bot agent service</param>
        /// <param name="hubContext">The SignalR hub context</param>
        /// <param name="botAgentId">ID of the target bot agent</param>
        /// <param name="command">Command to execute</param>
        /// <param name="payload">Additional data for the command</param>
        /// <param name="logger">Optional logger for the operation</param>
        public static async Task SendCommandToBotAgentAsync(
            this IBotAgentService botAgentService,
            IHubContext<BotAgentHub> hubContext,
            Guid botAgentId,
            string command,
            object payload,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(botAgentService);
            ArgumentNullException.ThrowIfNull(hubContext);
                
            // Send command to the specific bot agent group
            await hubContext.Clients
                .Group($"bot-{botAgentId}")
                .SendAsync("ReceiveCommand", command, payload);
                
            logger?.LogInformation(LogMessages.CommandSent, botAgentId, command);
        }
        
        /// <summary>
        /// Sends a notification to all frontend clients in a tenant
        /// </summary>
        /// <param name="botAgentService">The bot agent service</param>
        /// <param name="hubContext">The SignalR hub context</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="data">Notification data</param>
        /// <param name="tenantContext">Optional tenant context for validation</param>
        /// <param name="logger">Optional logger for the operation</param>
        public static async Task SendTenantNotificationAsync(
            this IBotAgentService botAgentService,
            IHubContext<BotAgentHub> hubContext,
            Guid tenantId,
            string notificationType,
            object data,
            ITenantContext? tenantContext = null,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(botAgentService);
            ArgumentNullException.ThrowIfNull(hubContext);
                
            // Validate tenant context if provided
            if (tenantContext != null && tenantContext.HasTenant)
            {
                // Ensure operation is only performed for the current tenant
                if (tenantContext.CurrentTenantId != tenantId)
                {
                    logger?.LogWarning(LogMessages.CrossTenantNotification, tenantContext.CurrentTenantId, tenantId);
                    throw new UnauthorizedAccessException("Attempted cross-tenant notification");
                }
                
                logger?.LogDebug(LogMessages.SendingNotification, tenantId, notificationType);
            }
                
            // Send notification to all clients in the tenant group
            await hubContext.Clients
                .Group($"tenant-{tenantId}")
                .SendAsync(notificationType, data);
        }
    }
} 