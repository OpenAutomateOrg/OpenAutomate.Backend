using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for managing Assets
    /// </summary>
    public interface IAssetService
    {
        /// <summary>
        /// Creates a new Asset
        /// </summary>
        /// <param name="dto">The Asset creation data</param>
        /// <returns>The created Asset</returns>
        Task<AssetResponseDto> CreateAssetAsync(CreateAssetDto dto);
        
        /// <summary>
        /// Gets all Assets for the current tenant
        /// </summary>
        /// <returns>Collection of Assets</returns>
        Task<IEnumerable<AssetListResponseDto>> GetAllAssetsAsync();
        
        /// <summary>
        /// Gets an Asset by its ID
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <returns>The Asset if found, null otherwise</returns>
        Task<AssetResponseDto?> GetAssetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets an Asset by its key
        /// </summary>
        /// <param name="key">The Asset key</param>
        /// <returns>The Asset if found, null otherwise</returns>
        Task<AssetResponseDto?> GetAssetByKeyAsync(string key);
        
        /// <summary>
        /// Updates an existing Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <param name="dto">The updated Asset data</param>
        /// <returns>The updated Asset</returns>
        Task<AssetResponseDto> UpdateAssetAsync(Guid id, UpdateAssetDto dto);
        
        /// <summary>
        /// Deletes an Asset
        /// </summary>
        /// <param name="id">The Asset ID</param>
        /// <returns>True if deleted, False if not found</returns>
        Task<bool> DeleteAssetAsync(Guid id);
        
        /// <summary>
        /// Gets all Bot Agents authorized to access an Asset
        /// </summary>
        /// <param name="assetId">The Asset ID</param>
        /// <returns>Collection of authorized Bot Agents</returns>
        Task<IEnumerable<BotAgentSummaryDto>> GetAuthorizedBotAgentsAsync(Guid assetId);
        
        /// <summary>
        /// Updates the Bot Agents authorized to access an Asset
        /// </summary>
        /// <param name="assetId">The Asset ID</param>
        /// <param name="dto">The list of Bot Agent IDs</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateAuthorizedBotAgentsAsync(Guid assetId, AssetBotAgentDto dto);
        
        /// <summary>
        /// Authorizes a Bot Agent to access an Asset
        /// </summary>
        /// <param name="assetId">The Asset ID</param>
        /// <param name="botAgentId">The Bot Agent ID</param>
        /// <returns>True if authorized successfully</returns>
        Task<bool> AuthorizeBotAgentAsync(Guid assetId, Guid botAgentId);
        
        /// <summary>
        /// Revokes a Bot Agent's access to an Asset
        /// </summary>
        /// <param name="assetId">The Asset ID</param>
        /// <param name="botAgentId">The Bot Agent ID</param>
        /// <returns>True if revoked successfully</returns>
        Task<bool> RevokeBotAgentAsync(Guid assetId, Guid botAgentId);
        
        /// <summary>
        /// Gets an Asset value by key for a Bot Agent using machine key authentication
        /// </summary>
        /// <param name="key">The Asset key</param>
        /// <param name="machineKey">The Bot Agent machine key</param>
        /// <returns>The Asset value if authorized, null otherwise</returns>
        Task<string?> GetAssetValueForBotAgentAsync(string key, string machineKey);
        
        /// <summary>
        /// Gets all Assets accessible by a Bot Agent using machine key authentication
        /// </summary>
        /// <param name="machineKey">The Bot Agent machine key</param>
        /// <returns>Collection of accessible Assets</returns>
        Task<IEnumerable<AssetListResponseDto>?> GetAccessibleAssetsForBotAgentAsync(string machineKey);
        
        /// <summary>
        /// Exports all Assets to CSV format
        /// </summary>
        /// <param name="includeSecrets">Whether to include actual secret values or use placeholders (default: false for security)</param>
        /// <returns>CSV content as byte array</returns>
        Task<byte[]> ExportAssetsToCsvAsync(bool includeSecrets = false);
        
        /// <summary>
        /// Imports Assets from CSV data
        /// </summary>
        /// <param name="csvData">CSV file content as byte array</param>
        /// <returns>Import result with statistics and errors</returns>
        Task<CsvImportResultDto> ImportAssetsFromCsvAsync(byte[] csvData);
    }
} 