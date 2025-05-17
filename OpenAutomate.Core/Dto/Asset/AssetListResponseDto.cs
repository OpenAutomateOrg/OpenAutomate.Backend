using System;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for Asset list response data (without sensitive values)
    /// </summary>
    public class AssetListResponseDto
    {
        /// <summary>
        /// Unique identifier for the Asset
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The type of the Asset (String or Secret)
        /// </summary>
        public AssetType Type { get; set; }
        
        /// <summary>
        /// Whether the Asset value is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }
        
        /// <summary>
        /// When the Asset was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the Asset was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }
        
        /// <summary>
        /// Number of Bot Agents authorized to access this Asset
        /// </summary>
        public int AuthorizedBotAgentsCount { get; set; }
    }
} 