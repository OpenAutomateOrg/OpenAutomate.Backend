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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

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
        
        #region CSV Import/Export Methods
        
        /// <summary>
        /// Exports all Assets to CSV format
        /// </summary>
        /// <returns>CSV content as byte array</returns>
        public async Task<byte[]> ExportAssetsToCsvAsync()
        {
            try
            {
                _logger.LogInformation("Exporting assets to CSV for tenant {TenantId}", _tenantContext.CurrentTenantId);
                
                var assets = await _context.Assets
                    .Include(a => a.AssetBotAgents)
                    .ThenInclude(aba => aba.BotAgent)
                    .Where(a => a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .ToListAsync();
                
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);
                using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
                
                // Write header
                csv.WriteField("Key");
                csv.WriteField("Value");
                csv.WriteField("Description");
                csv.WriteField("Type");
                csv.WriteField("BotAgentNames");
                csv.NextRecord();
                
                // Write data
                foreach (var asset in assets)
                {
                    csv.WriteField(asset.Key);
                    csv.WriteField(asset.IsEncrypted ? DecryptValue(asset.Value) : asset.Value);
                    csv.WriteField(asset.Description);
                    csv.WriteField(asset.IsEncrypted ? "Secret" : "String");
                    
                    var botAgentNames = string.Join(",", asset.AssetBotAgents.Select(aba => aba.BotAgent.Name));
                    csv.WriteField(botAgentNames);
                    csv.NextRecord();
                }
                
                writer.Flush();
                _logger.LogInformation("Successfully exported {Count} assets to CSV", assets.Count);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting assets to CSV for tenant {TenantId}: {Message}", 
                    _tenantContext.CurrentTenantId, ex.Message);
                throw new AssetException("Error exporting assets to CSV", ex);
            }
        }
        
        /// <summary>
        /// Imports Assets from CSV data
        /// </summary>
        /// <param name="csvData">CSV file content as byte array</param>
        /// <returns>Import result with statistics and errors</returns>
        public async Task<CsvImportResultDto> ImportAssetsFromCsvAsync(byte[] csvData)
        {
            var result = new CsvImportResultDto();
            
            try
            {
                _logger.LogInformation("Starting CSV import for tenant {TenantId}", _tenantContext.CurrentTenantId);
                
                using var memoryStream = new MemoryStream(csvData);
                using var reader = new StreamReader(memoryStream);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
                
                // Read and validate headers
                if (!csv.Read())
                {
                    result.Errors.Add("CSV file is empty or cannot be read");
                    return result;
                }
                
                csv.ReadHeader();
                var headers = csv.HeaderRecord;
                
                var headerValidation = ValidateCsvHeadersDetailed(headers);
                if (!headerValidation.IsValid)
                {
                    result.Errors.Add($"Invalid CSV format. {headerValidation.ErrorMessage}");
                    if (headerValidation.MissingHeaders.Count > 0)
                    {
                        result.Errors.Add($"Missing required headers: {string.Join(", ", headerValidation.MissingHeaders)}");
                    }
                    return result;
                }
                
                // Get existing bot agents for name lookup
                var botAgents = await _context.BotAgents
                    .Where(ba => ba.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .ToListAsync();
                
                var botAgentNameToId = botAgents.ToDictionary(ba => ba.Name.ToLower(), ba => ba.Id);
                
                // Get existing assets for update/create logic
                var existingAssetsDict = await _context.Assets
                    .Where(a => a.OrganizationUnitId == _tenantContext.CurrentTenantId)
                    .ToDictionaryAsync(a => a.Key.ToLower(), a => a);
                
                var rowNumber = 1;
                var assetsToCreate = new List<Asset>();
                var assetsToUpdate = new List<Asset>();
                var assetBotAgentRelations = new List<(Asset asset, List<Guid> botAgentIds)>();
                var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                while (csv.Read())
                {
                    rowNumber++;
                    result.TotalRows++;
                    
                    try
                    {
                        // Try to get fields with proper error handling
                        string? keyField = null;
                        string? valueField = null;
                        string? descriptionField = null;
                        string? typeField = null;
                        string? botAgentNamesField = null;
                        
                        try
                        {
                            keyField = csv.GetField("Key");
                            valueField = csv.GetField("Value");
                            descriptionField = csv.GetField("Description");
                            typeField = csv.GetField("Type");
                            botAgentNamesField = csv.GetField("BotAgentNames");
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Row {rowNumber}: Error reading CSV fields - {ex.Message}");
                            result.FailedImports++;
                            continue;
                        }
                        
                        var csvRecord = new AssetCsvDto
                        {
                            Key = keyField?.Trim() ?? string.Empty,
                            Value = valueField?.Trim() ?? string.Empty,
                            Description = descriptionField?.Trim() ?? string.Empty,
                            Type = typeField?.Trim() ?? "String",
                            BotAgentNames = botAgentNamesField?.Trim() ?? string.Empty
                        };
                        
                        // Additional validation for empty required fields
                        if (string.IsNullOrWhiteSpace(csvRecord.Key))
                        {
                            result.Errors.Add($"Row {rowNumber}: Key field is required and cannot be empty");
                            result.FailedImports++;
                            continue;
                        }
                        
                        if (string.IsNullOrWhiteSpace(csvRecord.Value))
                        {
                            result.Errors.Add($"Row {rowNumber}: Value field is required and cannot be empty");
                            result.FailedImports++;
                            continue;
                        }
                        
                        var validationErrors = ValidateCsvRecord(csvRecord, rowNumber);
                        if (validationErrors.Count > 0)
                        {
                            result.Errors.AddRange(validationErrors);
                            result.FailedImports++;
                            continue;
                        }
                        
                        // Check for duplicate key within the same import
                        var keyLower = csvRecord.Key.ToLower();
                        if (processedKeys.Contains(keyLower))
                        {
                            result.Errors.Add($"Row {rowNumber}: Duplicate key '{csvRecord.Key}' found within the import file");
                            result.FailedImports++;
                            continue;
                        }
                        processedKeys.Add(keyLower);
                        
                        // Parse asset type
                        var assetType = csvRecord.Type.ToLower() switch
                        {
                            "secret" => AssetType.Secret,
                            "string" => AssetType.String,
                            _ => AssetType.String
                        };
                        
                        var isEncrypted = assetType == AssetType.Secret;
                        Asset asset;
                        bool isUpdate = false;
                        
                        // Check if asset exists - update if yes, create if no
                        if (existingAssetsDict.TryGetValue(keyLower, out var existingAsset))
                        {
                            // Update existing asset
                            asset = existingAsset;
                            asset.Value = isEncrypted ? EncryptValue(csvRecord.Value) : csvRecord.Value;
                            asset.Description = csvRecord.Description;
                            asset.IsEncrypted = isEncrypted;
                            asset.LastModifyAt = DateTime.UtcNow;
                            isUpdate = true;
                            
                            result.Warnings.Add($"Row {rowNumber}: Updated existing asset '{csvRecord.Key}' (Type: {csvRecord.Type})");
                        }
                        else
                        {
                            // Create new asset
                            asset = new Asset
                            {
                                Id = Guid.NewGuid(),
                                Key = csvRecord.Key,
                                Value = isEncrypted ? EncryptValue(csvRecord.Value) : csvRecord.Value,
                                Description = csvRecord.Description,
                                IsEncrypted = isEncrypted,
                                OrganizationUnitId = _tenantContext.CurrentTenantId,
                                CreatedAt = DateTime.UtcNow
                            };
                        }
                        
                        // Parse bot agent names - only validate if provided
                        var botAgentIds = new List<Guid>();
                        var hasInvalidBotAgents = false;
                        
                        if (!string.IsNullOrEmpty(csvRecord.BotAgentNames))
                        {
                            var names = csvRecord.BotAgentNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(n => n.Trim())
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .ToList();
                            
                            // Only validate if there are actual names to process
                            if (names.Count > 0)
                            {
                                foreach (var name in names)
                                {
                                    if (botAgentNameToId.TryGetValue(name.ToLower(), out var botAgentId))
                                    {
                                        botAgentIds.Add(botAgentId);
                                    }
                                    else
                                    {
                                        result.Errors.Add($"Row {rowNumber}: Bot Agent '{name}' not found. Please check the bot agent name.");
                                        hasInvalidBotAgents = true;
                                    }
                                }
                            }
                        }
                        
                        // Skip this row if there are invalid bot agents (only when bot agents were specified)
                        if (hasInvalidBotAgents)
                        {
                            result.FailedImports++;
                            continue;
                        }
                        
                        // Add to appropriate list for processing
                        if (isUpdate)
                        {
                            assetsToUpdate.Add(asset);
                        }
                        else
                        {
                            assetsToCreate.Add(asset);
                        }
                        
                        assetBotAgentRelations.Add((asset, botAgentIds));
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {rowNumber}: Error processing row - {ex.Message}");
                        result.FailedImports++;
                    }
                }
                
                // Save assets to database
                var totalProcessed = 0;
                
                // Add new assets
                if (assetsToCreate.Count > 0)
                {
                    _context.Assets.AddRange(assetsToCreate);
                    totalProcessed += assetsToCreate.Count;
                }
                
                // Update existing assets (assetsToUpdate are already tracked by EF)
                if (assetsToUpdate.Count > 0)
                {
                    _context.Assets.UpdateRange(assetsToUpdate);
                    totalProcessed += assetsToUpdate.Count;
                }
                
                // Save all changes
                if (totalProcessed > 0)
                {
                    await _context.SaveChangesAsync();
                    
                    // Handle bot agent relationships for both new and updated assets
                    foreach (var (asset, botAgentIds) in assetBotAgentRelations)
                    {
                        // Remove existing relationships for updated assets
                        var existingRelations = await _context.AssetBotAgents
                            .Where(aba => aba.AssetId == asset.Id)
                            .ToListAsync();
                        
                        if (existingRelations.Any())
                        {
                            _context.AssetBotAgents.RemoveRange(existingRelations);
                        }
                        
                        // Add new relationships
                        foreach (var botAgentId in botAgentIds)
                        {
                            _context.AssetBotAgents.Add(new AssetBotAgent
                            {
                                AssetId = asset.Id,
                                BotAgentId = botAgentId
                            });
                        }
                    }
                    
                    // Save bot agent relationships
                    if (assetBotAgentRelations.Any())
                    {
                        await _context.SaveChangesAsync();
                    }
                    
                    result.SuccessfulImports = totalProcessed;
                }
                
                _logger.LogInformation("CSV import completed for tenant {TenantId}. Success: {Success}, Failed: {Failed}", 
                    _tenantContext.CurrentTenantId, result.SuccessfulImports, result.FailedImports);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing assets from CSV for tenant {TenantId}: {Message}", 
                    _tenantContext.CurrentTenantId, ex.Message);
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }
        
        /// <summary>
        /// Validates CSV headers with detailed error information
        /// </summary>
        private static (bool IsValid, string ErrorMessage, List<string> MissingHeaders) ValidateCsvHeadersDetailed(string[]? headers)
        {
            var missingHeaders = new List<string>();
            
            if (headers == null || headers.Length == 0)
            {
                return (false, "CSV file has no headers", new List<string> { "Key", "Value", "Description", "Type", "BotAgentNames" });
            }
                
            // Only Key, Value, and Type are truly required
            var requiredHeaders = new[] { "Key", "Value", "Type" };
            var optionalHeaders = new[] { "Description", "BotAgentNames" };
            
            foreach (var requiredHeader in requiredHeaders)
            {
                if (!headers.Contains(requiredHeader, StringComparer.OrdinalIgnoreCase))
                {
                    missingHeaders.Add(requiredHeader);
                }
            }
            
            if (missingHeaders.Count > 0)
            {
                return (false, "Required headers are missing", missingHeaders);
            }
            
            return (true, string.Empty, new List<string>());
        }
        
        /// <summary>
        /// Validates CSV headers (legacy method for backward compatibility)
        /// </summary>
        private static bool ValidateCsvHeaders(string[]? headers)
        {
            var result = ValidateCsvHeadersDetailed(headers);
            return result.IsValid;
        }
        
        /// <summary>
        /// Validates a CSV record
        /// </summary>
        private static List<string> ValidateCsvRecord(AssetCsvDto record, int rowNumber)
        {
            var errors = new List<string>();
            
            // Key validation (required)
            if (string.IsNullOrWhiteSpace(record.Key))
                errors.Add($"Row {rowNumber}: Key is required");
            else if (record.Key.Length > 50)
                errors.Add($"Row {rowNumber}: Key must be 50 characters or less");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(record.Key, @"^[a-zA-Z0-9_\-.]+$"))
                errors.Add($"Row {rowNumber}: Key can only contain letters, numbers, underscores, hyphens, and periods");
            
            // Value validation (required)
            if (string.IsNullOrWhiteSpace(record.Value))
                errors.Add($"Row {rowNumber}: Value is required");
            
            // Description validation (optional - can be null or empty)
            if (!string.IsNullOrEmpty(record.Description) && record.Description.Length > 500)
                errors.Add($"Row {rowNumber}: Description must be 500 characters or less");
            
            // Type validation (optional - defaults to String if not provided)
            if (!string.IsNullOrEmpty(record.Type) && 
                !new[] { "String", "Secret" }.Contains(record.Type, StringComparer.OrdinalIgnoreCase))
                errors.Add($"Row {rowNumber}: Type must be either 'String' or 'Secret'");
            
            return errors;
        }
        
        #endregion
    }
} 