using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.Domain.Entities;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Interface for Bot Agent management and operations
    /// </summary>
    public interface IBotAgentService
    {
        /// <summary>
        /// Creates a new Bot Agent with a generated machine key
        /// </summary>
        /// <param name="dto">The Bot Agent creation data</param>
        /// <returns>The CreatedAtBot Agent with machine key</returns>
        Task<BotAgentResponseDto> CreateBotAgentAsync(CreateBotAgentDto dto);
        
        /// <summary>
        /// Gets a Bot Agent by ID
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <returns>The Bot Agent if found, null otherwise</returns>
        Task<BotAgentResponseDto> GetBotAgentByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all Bot Agents for the current tenant
        /// </summary>
        /// <returns>Collection of Bot Agents</returns>
        Task<IEnumerable<BotAgentResponseDto>> GetAllBotAgentsAsync();
        
        /// <summary>
        /// Regenerates the machine key for a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <returns>The updated Bot Agent with new machine key</returns>
        Task<BotAgentResponseDto> RegenerateMachineKeyAsync(Guid id);
        
        /// <summary>
        /// Resolves tenant from slug and sets it in the tenant context
        /// </summary>
        /// <param name="tenantSlug">The tenant slug to resolve</param>
        /// <returns>True if tenant was resolved successfully, false otherwise</returns>
        Task<bool> ResolveTenantFromSlugAsync(string tenantSlug);
        
        /// <summary>
        /// Deactivates a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        Task DeactivateBotAgentAsync(Guid id);

        /// <summary>
        /// Handles a bot agent connection, updating its status and timestamps
        /// </summary>
        /// <param name="machineKey">The machine key of the bot agent</param>
        /// <returns>The connected BotAgent entity or null if not found</returns>
        Task<BotAgent?> ConnectBotAgentAsync(string machineKey);

        /// <summary>
        /// Handles a bot agent disconnection, updating its status
        /// </summary>
        /// <param name="machineKey">The machine key of the bot agent</param>
        /// <returns>The disconnected BotAgent entity or null if not found</returns>
        Task<BotAgent?> DisconnectBotAgentAsync(string machineKey);

        /// <summary>
        /// Updates the status and heartbeat of a bot agent
        /// </summary>
        /// <param name="machineKey">The machine key of the bot agent</param>
        /// <param name="status">The new status</param>
        /// <param name="executionId">Optional execution ID</param>
        /// <returns>The updated BotAgent entity or null if not found</returns>
        Task<BotAgent?> UpdateBotAgentStatusAsync(string machineKey, string status, string? executionId = null);

        /// <summary>
        /// Updates the heartbeat of a bot agent (keep-alive)
        /// </summary>
        /// <param name="machineKey">The machine key of the bot agent</param>
        /// <returns>The updated BotAgent entity or null if not found</returns>
        Task<BotAgent?> KeepAliveAsync(string machineKey);

        /// <summary>
        /// Deletes a Bot Agent.
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        Task DeleteBotAgentAsync(Guid id);

        /// <summary>
        /// Updates a Bot Agent's editable fields
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        /// <param name="dto">The update data</param>
        /// <returns>The updated Bot Agent</returns>
        Task<BotAgentResponseDto> UpdateBotAgentAsync(Guid id, UpdateBotAgentDto dto);
        
        /// <summary>
        /// Deletes multiple Bot Agents in a single operation
        /// </summary>
        /// <param name="ids">List of Bot Agent IDs to delete</param>
        /// <returns>Result of the bulk delete operation</returns>
        Task<BulkDeleteResultDto> BulkDeleteBotAgentsAsync(List<Guid> ids);
    }
} 