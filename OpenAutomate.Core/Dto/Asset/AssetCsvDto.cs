using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Asset
{
    /// <summary>
    /// DTO for Asset CSV import/export operations
    /// </summary>
    public class AssetCsvDto
    {
        /// <summary>
        /// The unique key used to reference this Asset
        /// </summary>
        [Required]
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// The value of the Asset
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the Asset
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// The type of the Asset (String or Secret)
        /// </summary>
        [Required]
        public string Type { get; set; } = "String";
        

    }
}
