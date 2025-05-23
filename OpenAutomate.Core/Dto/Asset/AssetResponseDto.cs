using System;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for Asset response data
    /// </summary>
    public class AssetResponseDto
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
        /// The value of the Asset (may be encrypted)
        /// </summary>
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the Asset value is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// The type of the Asset (String or Secret)
        /// </summary>
        public AssetType Type { get; set; }
        
        /// <summary>
        /// When the Asset was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the Asset was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }
    }
} 