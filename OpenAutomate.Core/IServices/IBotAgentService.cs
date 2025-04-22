using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;

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
        /// <returns>The created Bot Agent with machine key</returns>
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
        /// Validates a Bot Agent connection request and updates its status
        /// </summary>
        /// <param name="request">The connection request</param>
        /// <param name="tenantSlug">The tenant slug from the URL</param>
        /// <returns>Bot Agent information if successfully connected</returns>
        Task<BotAgentResponseDto> ValidateAndConnectBotAgentAsync(BotAgentConnectionRequest request, string tenantSlug);
        
        /// <summary>
        /// Updates a Bot Agent's status
        /// </summary>
        /// <param name="request">The status update request</param>
        /// <param name="tenantSlug">The tenant slug from the URL</param>
        Task UpdateBotAgentStatusAsync(BotAgentStatusUpdateRequest request, string tenantSlug);
        
        /// <summary>
        /// Gets assets available to a specific Bot Agent
        /// </summary>
        /// <param name="machineKey">The Bot Agent's machine key</param>
        /// <param name="tenantSlug">The tenant slug from the URL</param>
        /// <returns>Collection of assets available to the Bot Agent</returns>
        Task<IEnumerable<AssetResponseDto>> GetAssetsForBotAgentAsync(string machineKey, string tenantSlug);
        
        /// <summary>
        /// Deactivates a Bot Agent
        /// </summary>
        /// <param name="id">The Bot Agent ID</param>
        Task DeactivateBotAgentAsync(Guid id);
    }
} 