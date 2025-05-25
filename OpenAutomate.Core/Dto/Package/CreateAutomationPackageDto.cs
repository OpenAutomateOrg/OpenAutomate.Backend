using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Package
{
    /// <summary>
    /// DTO for creating a new automation package
    /// </summary>
    public class CreateAutomationPackageDto
    {
        /// <summary>
        /// Package name
        /// </summary>
        [Required(ErrorMessage = "Package name is required")]
        [StringLength(100, ErrorMessage = "Package name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Package description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
    }
} 