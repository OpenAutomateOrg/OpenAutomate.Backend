using System.IO;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for extracting metadata from automation package files
    /// </summary>
    public interface IPackageMetadataService
    {
        /// <summary>
        /// Extracts package metadata from an uploaded file
        /// </summary>
        /// <param name="fileStream">The package file stream</param>
        /// <param name="fileName">The original filename</param>
        /// <returns>Extracted package metadata</returns>
        Task<PackageMetadata> ExtractMetadataAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Validates if the file is a valid automation package
        /// </summary>
        /// <param name="fileStream">The package file stream</param>
        /// <param name="fileName">The original filename</param>
        /// <returns>True if valid package</returns>
        Task<bool> IsValidPackageAsync(Stream fileStream, string fileName);
    }

    /// <summary>
    /// Metadata extracted from a package file
    /// </summary>
    public class PackageMetadata
    {
        /// <summary>
        /// Package name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Package description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Package version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Package author/creator
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Whether metadata was successfully extracted
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if extraction failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
} 