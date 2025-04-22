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
        /// The display name of the Asset
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// The value of the Asset (may be encrypted)
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the Asset value is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }
    }
} 