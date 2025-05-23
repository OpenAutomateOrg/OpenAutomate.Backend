using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Exceptions;
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
        
        // WARNING: Do NOT use hardcoded keys in production. Store securely in configuration or a secret manager.
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes for AES-256
        
        // Standardized log message templates
        private static class LogMessages
        {
            public const string AssetKeyExists = "Asset with key '{Key}' already exists for tenant {TenantId}";
            public const string AssetCreated = "Asset created: {Id}, Key: {Key}, Tenant: {TenantId}";
            public const string AssetCreationError = "Error creating asset: {Message}";
        }
        
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
                    _logger.LogWarning(LogMessages.AssetKeyExists, dto.Key, _tenantContext.CurrentTenantId);
                    throw new AssetKeyAlreadyExistsException(dto.Key, _tenantContext.CurrentTenantId);
                }
                
                // Create the new asset
                var asset = new Asset
                {
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
                if (dto.BotAgentIds != null && dto.BotAgentIds.Count > 0)
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
                
                _logger.LogInformation(LogMessages.AssetCreated, asset.Id, asset.Key, _tenantContext.CurrentTenantId);
                
                // Return the created asset
                return MapToResponseDto(asset);
            }
            catch (AssetException)
            {
                // Rethrow custom exceptions as they already have context
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.AssetCreationError, ex.Message);
                throw new ServiceException("Error creating asset", ex);
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
                throw new ServiceException($"Error retrieving assets for tenant {_tenantContext.CurrentTenantId}", ex);
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
                return MapToResponseDto(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by ID {Id}: {Message}", id, ex.Message);
                throw new ServiceException($"Error retrieving asset with ID {id} for tenant {_tenantContext.CurrentTenantId}", ex);
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
                return MapToResponseDto(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset by key {Key}: {Message}", key, ex.Message);
                throw new ServiceException($"Error retrieving asset with key '{key}' for tenant {_tenantContext.CurrentTenantId}", ex);
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
                asset.Key = dto.Key;
                asset.Description = dto.Description;
                asset.Value = asset.IsEncrypted ? EncryptValue(dto.Value) : dto.Value;
                asset.LastModifyAt = DateTime.UtcNow;
                
                // Save changes
                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();
                
                // Return the updated asset
                return MapToResponseDto(asset);
            }
            catch (KeyNotFoundException)
            {
                // Rethrow KeyNotFoundException as it already has proper context
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset with ID {Id}: {Message}", id, ex.Message);
                throw new ServiceException($"Error updating asset with ID {id} for tenant {_tenantContext.CurrentTenantId}", ex);
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
                throw new ServiceException($"Error deleting asset with ID {id} for tenant {_tenantContext.CurrentTenantId}", ex);
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
                    return false;
                }
                
                // Check if bot agent exists and belongs to the tenant
                var botAgent = await _context.BotAgents
                    .Where(b => b.Id == botAgentId && b.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (botAgent == null)
                {
                    return false;
                }
                
                // Find the relationship
                var relationship = await _context.AssetBotAgents
                    .Where(ab => 
                        ab.AssetId == assetId && 
                        ab.BotAgentId == botAgentId && 
                        ab.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .FirstOrDefaultAsync();
                    
                if (relationship == null)
                {
                    return false;
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
                throw new ServiceException($"Error revoking bot agent {botAgentId} for asset {assetId}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<string?> GetAssetValueForBotAgentAsync(string key, string machineKey)
        {
            try
            {
                // Find bot agent by machine key
                var botAgent = await _context.BotAgents
                    .FirstOrDefaultAsync(b => b.MachineKey == machineKey);
                    
                if (botAgent == null)
                {
                    _logger.LogWarning("Bot agent with machine key not found when requesting asset '{Key}'", key);
                    return null;
                }
                
                // Find asset by key in the same tenant as the bot agent
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => 
                        a.Key == key && 
                        a.OrganizationUnitId == botAgent.OrganizationUnitId);
                        
                if (asset == null)
                {
                    _logger.LogWarning("Asset with key '{Key}' not found for bot agent tenant", key);
                    return null;
                }
                
                // Check if bot agent is authorized to access this asset
                var isAuthorized = await _context.AssetBotAgents
                    .AnyAsync(aba => 
                        aba.AssetId == asset.Id && 
                        aba.BotAgentId == botAgent.Id);
                        
                if (!isAuthorized)
                {
                    _logger.LogWarning("Bot agent {BotAgentId} not authorized to access asset '{Key}'", 
                        botAgent.Id, key);
                    throw new UnauthorizedAccessException($"Bot agent not authorized to access asset '{key}'");
                }
                
                // Log access for audit purposes
                _logger.LogInformation("Bot agent {BotAgentName} ({BotAgentId}) accessed asset '{Key}'", 
                    botAgent.Name, botAgent.Id, key);
                
                // Return decrypted value if needed
                return asset.IsEncrypted ? DecryptValue(asset.Value) : asset.Value;
            }
            catch (UnauthorizedAccessException)
            {
                // Rethrow unauthorized exceptions for proper handling
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving asset value for key '{Key}' with machine key: {Message}", 
                    key, ex.Message);
                throw new ServiceException($"Error retrieving asset value for key '{key}'", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<AssetListResponseDto>?> GetAccessibleAssetsForBotAgentAsync(string machineKey)
        {
            try
            {
                // Find bot agent by machine key
                var botAgent = await _context.BotAgents
                    .FirstOrDefaultAsync(b => b.MachineKey == machineKey);
                    
                if (botAgent == null)
                {
                    _logger.LogWarning("Bot agent with machine key not found when requesting accessible assets");
                    return null;
                }
                
                // Find all assets accessible by this bot agent
                var assetIds = await _context.AssetBotAgents
                    .Where(aba => aba.BotAgentId == botAgent.Id)
                    .Select(aba => aba.AssetId)
                    .ToListAsync();
                    
                var assets = await _context.Assets
                    .Where(a => 
                        assetIds.Contains(a.Id) && 
                        a.OrganizationUnitId == botAgent.OrganizationUnitId)
                    .ToListAsync();
                    
                // Return asset list DTOs (without values)
                return assets.Select(a => new AssetListResponseDto
                {
                    Id = a.Id,
                    Key = a.Key,
                    Description = a.Description,
                    Type = a.IsEncrypted ? AssetType.Secret : AssetType.String,
                    IsEncrypted = a.IsEncrypted,
                    CreatedAt = a.CreatedAt ?? DateTime.UtcNow,
                    LastModifiedAt = a.LastModifyAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accessible assets with machine key: {Message}", ex.Message);
                throw new ServiceException("Error retrieving accessible assets for bot agent", ex);
            }
        }
        
        #region Helper Methods
        
        private AssetResponseDto MapToResponseDto(Asset asset)
        {
            return new AssetResponseDto
            {
                Id = asset.Id,
                Key = asset.Key,
                Value = asset.IsEncrypted ? DecryptValue(asset.Value) : asset.Value,
                Description = asset.Description,
                IsEncrypted = asset.IsEncrypted,
                Type = asset.IsEncrypted ? AssetType.Secret : AssetType.String,
                CreatedAt = asset.CreatedAt ?? DateTime.UtcNow,
                LastModifiedAt = asset.LastModifyAt
            };
        }
        
        /// <summary>
        /// Helper method to encrypt sensitive values
        /// </summary>
        private static string EncryptValue(string value)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = new byte[16]; // WARNING: Use a random IV in production and store it with the ciphertext
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AssetEncryptionException("Error encrypting asset value", ex);
            }
        }
        
        /// <summary>
        /// Helper method to decrypt sensitive values
        /// </summary>
        private static string DecryptValue(string encryptedValue)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = new byte[16]; // WARNING: Use the same IV as used for encryption in production
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
                throw new AssetEncryptionException("Error decrypting asset value", ex);
            }
        }
        
        #endregion
    }
} 