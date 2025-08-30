using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for updating an existing Asset
    /// </summary>
    public class UpdateAssetDto
    {
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
        /// The unique key used to reference this Asset
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9_\-.]+$", ErrorMessage = "Key can only contain letters, numbers, underscores, hyphens, and periods")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Whether this Asset is globally accessible to all agents in the organization unit
        /// </summary>
        public bool IsGlobal { get; set; }
    }
} 