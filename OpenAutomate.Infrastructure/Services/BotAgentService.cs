using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the Bot Agent service
    /// </summary>
    public class BotAgentService : IBotAgentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<BotAgentService> _logger;
        
        // Status constants - should match the ones in Core.Constants.AgentStatus
        private const string STATUS_AVAILABLE = "Available";
        private const string STATUS_BUSY = "Busy";
        private const string STATUS_DISCONNECTED = "Disconnected";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BotAgentService"/> class
        /// </summary>
        /// <param name="unitOfWork">The unit of work</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="logger">The logger</param>
        public BotAgentService(
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext,
            ILogger<BotAgentService> logger)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
            _logger = logger;
        }
        
        /// <inheritdoc />
        public async Task<BotAgentResponseDto> CreateBotAgentAsync(CreateBotAgentDto dto)
        {
           
            // Generate a unique machine key
            var machineKey = GenerateSecureMachineKey();
            
            var botAgent = new BotAgent
            {
                Name = dto.Name,
                MachineName = dto.MachineName,
                MachineKey = machineKey,
                Status = STATUS_DISCONNECTED,
                LastConnected = DateTime.UtcNow,
                IsActive = true,
                OrganizationUnitId = _tenantContext.CurrentTenantId
            };
            
            await _unitOfWork.BotAgents.AddAsync(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bot Agent created: {BotAgentId}, Name: {Name}, Machine: {MachineName}", 
                botAgent.Id, botAgent.Name, botAgent.MachineName);
                
            return MapToResponseDto(botAgent);
        }
        
        /// <inheritdoc />
        public async Task<BotAgentResponseDto> GetBotAgentByIdAsync(Guid id)
        {
            var botAgent = await _unitOfWork.BotAgents.GetByIdAsync(id);
            if (botAgent == null || botAgent.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                return null;
            }
            
            return MapToResponseDto(botAgent);
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<BotAgentResponseDto>> GetAllBotAgentsAsync()
        {
            var botAgents = await _unitOfWork.BotAgents.GetAllAsync(
                ba => ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
            return botAgents.Select(MapToResponseDto);
        }
        
        /// <inheritdoc />
        public async Task<BotAgentResponseDto> RegenerateMachineKeyAsync(Guid id)
        {
            var botAgent = await _unitOfWork.BotAgents.GetByIdAsync(id);
            if (botAgent == null || botAgent.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ApplicationException("Bot Agent not found");
            }
            
            // Generate a new machine key
            botAgent.MachineKey = GenerateSecureMachineKey();
            botAgent.Status = STATUS_DISCONNECTED; // Reset status as new key requires reconnection
            
            _unitOfWork.BotAgents.Update(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Machine key regenerated for Bot Agent: {BotAgentId}", botAgent.Id);
            
            return MapToResponseDto(botAgent);
        }

        /// <inheritdoc />
        public async Task<bool> ResolveTenantFromSlugAsync(string tenantSlug)
        {
            // Delegate to the tenant context
            return await _tenantContext.ResolveTenantFromSlugAsync(tenantSlug);
        }
        
        /// <inheritdoc />
        public async Task DeactivateBotAgentAsync(Guid id)
        {
            var botAgent = await _unitOfWork.BotAgents.GetByIdAsync(id);
            if (botAgent == null || botAgent.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ApplicationException("Bot Agent not found");
            }
            
            botAgent.IsActive = false;
            botAgent.Status = STATUS_DISCONNECTED;
            
            _unitOfWork.BotAgents.Update(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bot Agent deactivated: {BotAgentId}", botAgent.Id);
        }
        
        /// <summary>
        /// Generates a secure random machine key in UUID/GUID format
        /// </summary>
        /// <returns>A cryptographically secure UUID/GUID string</returns>
        private string GenerateSecureMachineKey()
        {
            return Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Maps a BotAgent entity to a BotAgentResponseDto
        /// </summary>
        /// <param name="botAgent">The Bot Agent entity</param>
        /// <returns>DTO representation of the Bot Agent</returns>
        private BotAgentResponseDto MapToResponseDto(BotAgent botAgent)
        {
            return new BotAgentResponseDto
            {
                Id = botAgent.Id,
                Name = botAgent.Name,
                MachineName = botAgent.MachineName,
                MachineKey = botAgent.MachineKey,
                Status = botAgent.Status,
                LastConnected = botAgent.LastConnected,
                IsActive = botAgent.IsActive
            };
        }

        public async Task<BotAgent?> ConnectBotAgentAsync(string machineKey)
        {
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (botAgent == null)
                return null;
            botAgent.Status = STATUS_AVAILABLE;
            botAgent.LastHeartbeat = DateTime.UtcNow;
            botAgent.LastConnected = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Bot agent connected: {BotAgentName} ({BotAgentId})", botAgent.Name, botAgent.Id);
            return botAgent;
        }

        public async Task<BotAgent?> DisconnectBotAgentAsync(string machineKey)
        {
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (botAgent == null)
                return null;
            botAgent.Status = STATUS_DISCONNECTED;
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Bot agent disconnected: {BotAgentName} ({BotAgentId})", botAgent.Name, botAgent.Id);
            return botAgent;
        }

        public async Task<BotAgent?> UpdateBotAgentStatusAsync(string machineKey, string status, string? executionId = null)
        {
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (botAgent == null)
                return null;
            botAgent.LastHeartbeat = DateTime.UtcNow;
            if (status == STATUS_AVAILABLE || status == STATUS_BUSY || status == STATUS_DISCONNECTED)
            {
                botAgent.Status = status;
            }
            await _unitOfWork.CompleteAsync();
            _logger.LogDebug("Bot status update: {BotAgentName} - {Status}", botAgent.Name, status);
            return botAgent;
        }

        public async Task<BotAgent?> KeepAliveAsync(string machineKey)
        {
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => ba.MachineKey == machineKey && ba.OrganizationUnitId == _tenantContext.CurrentTenantId);
            if (botAgent == null)
                return null;
            botAgent.LastHeartbeat = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            _logger.LogTrace("Keep-alive received from bot agent: {BotAgentName} ({BotAgentId})", botAgent.Name, botAgent.Id);
            return botAgent;
        }

        public async Task DeleteBotAgentAsync(Guid id)
        {
            var botAgent = await _unitOfWork.BotAgents.GetByIdAsync(id);
            if (botAgent == null || botAgent.OrganizationUnitId != _tenantContext.CurrentTenantId)
                throw new ApplicationException("Bot Agent not found");

            if (botAgent.Status != "Disconnected")
                throw new InvalidOperationException("You can only delete an agent when its status is 'Disconnected'.");

            var assetLinks = (await _unitOfWork.AssetBotAgents.GetAllAsync(
                x => x.BotAgentId == id && x.OrganizationUnitId == _tenantContext.CurrentTenantId)).ToList();
            _unitOfWork.AssetBotAgents.RemoveRange(assetLinks);

            _unitOfWork.BotAgents.Remove(botAgent);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Bot Agent deleted: {BotAgentId}", botAgent.Id);
        }
    }
} 