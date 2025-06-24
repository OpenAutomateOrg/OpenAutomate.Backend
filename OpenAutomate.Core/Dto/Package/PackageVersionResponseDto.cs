using System;

namespace OpenAutomate.Core.Dto.Package
{
    /// <summary>
    /// Response DTO for package version
    /// </summary>
    public class PackageVersionResponseDto
    {
        /// <summary>
        /// Version unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Original filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Content type of the file
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the version is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the version was uploaded
        /// </summary>
        public DateTime UploadedAt { get; set; }
    }
} 