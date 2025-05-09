using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for updating an existing Asset
    /// </summary>
    public class UpdateAssetDto
    {
        /// <summary>
        /// The display name of the Asset
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
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
    }
} 