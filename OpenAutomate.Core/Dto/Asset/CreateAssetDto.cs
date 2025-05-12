using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for creating a new Asset
    /// </summary>
    public class CreateAssetDto
    {
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9_\-.]+$", ErrorMessage = "Key can only contain letters, numbers, underscores, hyphens, and periods")]
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// The value of the Asset
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The type of the Asset (String or Secret)
        /// </summary>
        [Required]
        public AssetType Type { get; set; } = AssetType.String;
        
        /// <summary>
        /// Optional list of Bot Agent IDs that can access this Asset
        /// </summary>
        public List<Guid>? BotAgentIds { get; set; }
    }
    
    /// <summary>
    /// Enum defining the types of assets
    /// </summary>
    public enum AssetType
    {
        /// <summary>
        /// Regular string asset (stored as plaintext)
        /// </summary>
        String = 0,
        
        /// <summary>
        /// Secret asset (stored encrypted)
        /// </summary>
        Secret = 1
    }
} 