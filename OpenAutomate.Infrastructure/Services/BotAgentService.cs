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
                Status = "Disconnected",
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
            botAgent.Status = "Pending"; // Reset status as new key requires reconnection
            
            _unitOfWork.BotAgents.Update(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Machine key regenerated for Bot Agent: {BotAgentId}", botAgent.Id);
            
            return MapToResponseDto(botAgent);
        }
        
        /// <inheritdoc />
        public async Task<BotAgentResponseDto> ValidateAndConnectBotAgentAsync(
            BotAgentConnectionRequest request, 
            string tenantSlug)
        {
            // Lookup tenant by slug
            var tenant = await _unitOfWork.OrganizationUnits
                .GetFirstOrDefaultAsync(ou => ou.Slug == tenantSlug && ou.IsActive);
                
            if (tenant == null)
            {
                throw new ApplicationException($"Tenant '{tenantSlug}' not found or inactive");
            }
            
            // Find Bot Agent by machine key within tenant
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => 
                    ba.MachineKey == request.MachineKey && 
                    ba.OrganizationUnitId == tenant.Id &&
                    ba.IsActive);
                    
            if (botAgent == null)
            {
                throw new UnauthorizedAccessException("Invalid machine key or Bot Agent is inactive");
            }
            
            // Update Bot Agent information
            botAgent.Status = "Online";
            botAgent.LastConnected = DateTime.UtcNow;
            
            _unitOfWork.BotAgents.Update(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bot Agent {BotAgentId} successfully connected from {MachineName}", 
                botAgent.Id, request.MachineName);
                
            return MapToResponseDto(botAgent);
        }
        
        /// <inheritdoc />
        public async Task UpdateBotAgentStatusAsync(BotAgentStatusUpdateRequest request, string tenantSlug)
        {
            // Lookup tenant by slug
            var tenant = await _unitOfWork.OrganizationUnits
                .GetFirstOrDefaultAsync(ou => ou.Slug == tenantSlug && ou.IsActive);
                
            if (tenant == null)
            {
                throw new ApplicationException($"Tenant '{tenantSlug}' not found or inactive");
            }
            
            // Find Bot Agent by machine key within tenant
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => 
                    ba.MachineKey == request.MachineKey && 
                    ba.OrganizationUnitId == tenant.Id &&
                    ba.IsActive);
                    
            if (botAgent == null)
            {
                throw new UnauthorizedAccessException("Invalid machine key or Bot Agent is inactive");
            }
            
            // Update Bot Agent status
            botAgent.Status = request.Status;
            botAgent.LastConnected = request.Timestamp;
            
            _unitOfWork.BotAgents.Update(botAgent);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bot Agent {BotAgentId} status updated to {Status}", 
                botAgent.Id, request.Status);
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<AssetResponseDto>> GetAssetsForBotAgentAsync(string machineKey, string tenantSlug)
        {
            // Lookup tenant by slug
            var tenant = await _unitOfWork.OrganizationUnits
                .GetFirstOrDefaultAsync(ou => ou.Slug == tenantSlug && ou.IsActive);
                
            if (tenant == null)
            {
                throw new ApplicationException($"Tenant '{tenantSlug}' not found or inactive");
            }
            
            // Find Bot Agent by machine key within tenant
            var botAgent = await _unitOfWork.BotAgents
                .GetFirstOrDefaultAsync(ba => 
                    ba.MachineKey == machineKey && 
                    ba.OrganizationUnitId == tenant.Id &&
                    ba.IsActive,
                    ba => ba.AssetBotAgents);
                    
            if (botAgent == null)
            {
                throw new UnauthorizedAccessException("Invalid machine key or Bot Agent is inactive");
            }
            
            // Get associated assets
            if (botAgent.AssetBotAgents == null || !botAgent.AssetBotAgents.Any())
            {
                return Enumerable.Empty<AssetResponseDto>();
            }
            
            var assetIds = botAgent.AssetBotAgents.Select(aba => aba.AssetId).ToList();
            
            var assets = await _unitOfWork.Assets.GetAllAsync(
                a => assetIds.Contains(a.Id) && a.OrganizationUnitId == tenant.Id);
                
            return assets.Select(MapToAssetResponseDto);
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
            botAgent.Status = "Deactivated";
            
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
        
        /// <summary>
        /// Maps an Asset entity to an AssetResponseDto
        /// </summary>
        /// <param name="asset">The Asset entity</param>
        /// <returns>DTO representation of the Asset</returns>
        private AssetResponseDto MapToAssetResponseDto(Asset asset)
        {
            return new AssetResponseDto
            {
                Id = asset.Id,
                Name = asset.Name,
                Key = asset.Key,
                Value = asset.Value,
                Description = asset.Description,
                IsEncrypted = asset.IsEncrypted
            };
        }
    }
} 