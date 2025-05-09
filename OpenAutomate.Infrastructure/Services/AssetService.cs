using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing Assets
    /// </summary>
    public class AssetService : IAssetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<AssetService> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetService"/> class
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="logger">The logger</param>
        public AssetService(
            ApplicationDbContext context,
            ITenantContext tenantContext,
            ILogger<AssetService> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }
        
        /// <inheritdoc />
        public async Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto)
        {
            try
            {
                // Check if key already exists for this tenant
                var keyExists = await _context.Assets.AnyAsync(a => 
                    a.Key == dto.Key && 
                    a.OrganizationUnitId == _tenantContext.CurrentTenantId);
                    
                if (keyExists)
                {
                    throw new InvalidOperationException($"Asset with key '{dto.Key}' already exists");
                }
                
                // Create the new asset
                var asset = new Asset
                {
                    Name = dto.Name,
                    Key = dto.Key,
                    Description = dto.Description,
                    Value = dto.Type == AssetType.Secret ? EncryptValue(dto.Value) : dto.Value,
                    IsEncrypted = dto.Type == AssetType.Secret,
                    OrganizationUnitId = _tenantContext.CurrentTenantId,
                    CreatedAt = DateTime.UtcNow
                };
                
                // Add the asset to the database
                await _context.Assets.AddAsync(asset);
                await _context.SaveChangesAsync();
                
                // If bot agent IDs are provided, authorize them
                if (dto.BotAgentIds != null && dto.BotAgentIds.Any())
                {
                    foreach (var botAgentId in dto.BotAgentIds)
                    {
                        // Check if bot agent exists and belongs to the tenant
                        var botAgentExists = await _context.BotAgents.AnyAsync(b => 
                            b.Id == botAgentId && 
                            b.OrganizationUnitId == _tenantContext.CurrentTenantId);
                            
                        if (botAgentExists)
                        {
                            await _context.AssetBotAgents.AddAsync(new AssetBotAgent
                            {
                                AssetId = asset.Id,
                                BotAgentId = botAgentId,
                                OrganizationUnitId = _tenantContext.CurrentTenantId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                // Return the created asset
                return await MapToResponseDtoAsync(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating asset: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<AssetListResponseDto>> GetAllAssetsAsync()
        {
            try
            {
                // Get all assets for the current tenant
                var assets = await _context.Assets
                    .Where(a => a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .Include(a => a.AssetBotAgents)
                    .ToListAsync();
                    
                // Map to response DTOs
                return assets.Select(a => new AssetListResponseDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Key = a.Key,
                    Description = a.Description,
                    Type = a.IsEncrypted ? AssetType.Secret : AssetType.String,
                    IsEncrypted = a.IsEncrypted,
                    CreatedAt = a.CreatedAt ?? DateTime.UtcNow,
                    LastModifiedAt = a.LastModifyAt,
                    AuthorizedBotAgentsCount = a.AssetBotAgents?.Count ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all assets: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<AssetResponseDto?> GetAssetByIdAsync(Guid id)
        {
            try
            {
                // Get the asset by ID
                var asset = await _context.Assets
                    .Where(a => a.Id == id && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    return null;
                }
                
                // Return the asset
                return await MapToResponseDtoAsync(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<AssetResponseDto?> GetAssetByKeyAsync(string key)
        {
            try
            {
                // Get the asset by key
                var asset = await _context.Assets
                    .Where(a => a.Key == key && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    return null;
                }
                
                // Return the asset
                return await MapToResponseDtoAsync(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by key {Key}: {Message}", key, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<AssetResponseDto> UpdateAssetAsync(Guid id, UpdateAssetDto dto)
        {
            try
            {
                // Get the asset by ID
                var asset = await _context.Assets
                    .Where(a => a.Id == id && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Asset with ID {id} not found");
                }
                
                // Update the asset
                asset.Name = dto.Name;
                asset.Description = dto.Description;
                asset.Value = asset.IsEncrypted ? EncryptValue(dto.Value) : dto.Value;
                asset.LastModifyAt = DateTime.UtcNow;
                
                // Save changes
                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();
                
                // Return the updated asset
                return await MapToResponseDtoAsync(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset with ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> DeleteAssetAsync(Guid id)
        {
            try
            {
                // Get the asset by ID
                var asset = await _context.Assets
                    .Where(a => a.Id == id && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    return false;
                }
                
                // Delete related asset-bot agent relationships
                var assetBotAgents = await _context.AssetBotAgents
                    .Where(a => a.AssetId == id)
                    .ToListAsync();
                    
                _context.AssetBotAgents.RemoveRange(assetBotAgents);
                
                // Delete the asset
                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting asset with ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<BotAgentSummaryDto>> GetAuthorizedBotAgentsAsync(Guid assetId)
        {
            try
            {
                // Check if asset exists and belongs to the tenant
                var assetExists = await _context.Assets
                    .AnyAsync(a => a.Id == assetId && a.OrganizationUnitId == _tenantContext.CurrentTenantId);
                    
                if (!assetExists)
                {
                    throw new KeyNotFoundException($"Asset with ID {assetId} not found");
                }
                
                // Get the authorized bot agents
                var botAgentIds = await _context.AssetBotAgents
                    .Where(a => a.AssetId == assetId)
                    .Select(a => a.BotAgentId)
                    .ToListAsync();
                    
                // Get bot agent details
                var botAgents = await _context.BotAgents
                    .Where(b => botAgentIds.Contains(b.Id))
                    .ToListAsync();
                    
                // Map to DTOs
                return botAgents.Select(b => new BotAgentSummaryDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    MachineName = b.MachineName,
                    Status = b.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorized bot agents for asset {AssetId}: {Message}", assetId, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> UpdateAuthorizedBotAgentsAsync(Guid assetId, AssetBotAgentDto dto)
        {
            try
            {
                // Check if asset exists and belongs to the tenant
                var asset = await _context.Assets
                    .Where(a => a.Id == assetId && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Asset with ID {assetId} not found");
                }
                
                // Get existing relationships
                var existingRelationships = await _context.AssetBotAgents
                    .Where(a => a.AssetId == assetId)
                    .ToListAsync();
                    
                // Remove all existing relationships
                _context.AssetBotAgents.RemoveRange(existingRelationships);
                
                // Add new relationships
                foreach (var botAgentId in dto.BotAgentIds)
                {
                    // Check if bot agent exists and belongs to the tenant
                    var botAgentExists = await _context.BotAgents
                        .AnyAsync(b => b.Id == botAgentId && b.OrganizationUnitId == _tenantContext.CurrentTenantId);
                        
                    if (botAgentExists)
                    {
                        await _context.AssetBotAgents.AddAsync(new AssetBotAgent
                        {
                            AssetId = assetId,
                            BotAgentId = botAgentId,
                            OrganizationUnitId = _tenantContext.CurrentTenantId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Bot agent with ID {BotAgentId} not found or not in tenant", botAgentId);
                    }
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating authorized bot agents for asset {AssetId}: {Message}", assetId, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> AuthorizeBotAgentAsync(Guid assetId, Guid botAgentId)
        {
            try
            {
                // Check if asset exists and belongs to the tenant
                var asset = await _context.Assets
                    .Where(a => a.Id == assetId && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Asset with ID {assetId} not found");
                }
                
                // Check if bot agent exists and belongs to the tenant
                var botAgent = await _context.BotAgents
                    .Where(b => b.Id == botAgentId && b.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (botAgent == null)
                {
                    throw new KeyNotFoundException($"Bot agent with ID {botAgentId} not found");
                }
                
                // Check if relationship already exists
                var relationshipExists = await _context.AssetBotAgents
                    .AnyAsync(a => a.AssetId == assetId && a.BotAgentId == botAgentId);
                    
                if (relationshipExists)
                {
                    return true; // Already authorized
                }
                
                // Create new relationship
                await _context.AssetBotAgents.AddAsync(new AssetBotAgent
                {
                    AssetId = assetId,
                    BotAgentId = botAgentId,
                    OrganizationUnitId = _tenantContext.CurrentTenantId,
                    CreatedAt = DateTime.UtcNow
                });
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authorizing bot agent {BotAgentId} for asset {AssetId}: {Message}", 
                    botAgentId, assetId, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> RevokeBotAgentAsync(Guid assetId, Guid botAgentId)
        {
            try
            {
                // Check if asset exists and belongs to the tenant
                var asset = await _context.Assets
                    .Where(a => a.Id == assetId && a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Asset with ID {assetId} not found");
                }
                
                // Find the relationship
                var relationship = await _context.AssetBotAgents
                    .Where(a => a.AssetId == assetId && a.BotAgentId == botAgentId)
                    .FirstOrDefaultAsync();
                    
                if (relationship == null)
                {
                    return false; // Relationship doesn't exist
                }
                
                // Remove the relationship
                _context.AssetBotAgents.Remove(relationship);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking bot agent {BotAgentId} for asset {AssetId}: {Message}", 
                    botAgentId, assetId, ex.Message);
                throw;
            }
        }
        
        #region Helper Methods
        
        private async Task<AssetResponseDto> MapToResponseDtoAsync(Asset asset)
        {
            return new AssetResponseDto
            {
                Id = asset.Id,
                Name = asset.Name,
                Key = asset.Key,
                Description = asset.Description,
                Value = asset.IsEncrypted ? DecryptValue(asset.Value) : asset.Value,
                IsEncrypted = asset.IsEncrypted,
                Type = asset.IsEncrypted ? AssetType.Secret : AssetType.String,
                CreatedAt = asset.CreatedAt ?? DateTime.UtcNow,
                LastModifiedAt = asset.LastModifyAt
            };
        }
        
        private string EncryptValue(string value)
        {
            // Simple encryption for demo purposes
            // In a production environment, use a more secure approach with proper key management
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes("YourEncryptionKey12345678901234567890");
                aes.IV = new byte[16]; // Using a zero IV for simplicity - not secure for production
                
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] valueBytes = Encoding.UTF8.GetBytes(value);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        
        private string DecryptValue(string encryptedValue)
        {
            // Simple decryption for demo purposes
            // In a production environment, use a more secure approach with proper key management
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes("YourEncryptionKey12345678901234567890");
                    aes.IV = new byte[16]; // Using a zero IV for simplicity - not secure for production
                    
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting value: {Message}", ex.Message);
                return "[Decryption Error]";
            }
        }
        
        #endregion
    }
} 