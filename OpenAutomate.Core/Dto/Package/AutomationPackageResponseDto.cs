using System;
using System.Collections.Generic;

namespace OpenAutomate.Core.Dto.Package
{
    /// <summary>
    /// Response DTO for automation package
    /// </summary>
    public class AutomationPackageResponseDto
    {
        /// <summary>
        /// Package unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Package name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Package description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the package is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the package was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Package versions
        /// </summary>
        public List<PackageVersionResponseDto> Versions { get; set; } = new();
    }
} 