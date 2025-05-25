using OpenAutomate.Core.Dto.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for automation package management
    /// </summary>
    public interface IAutomationPackageService
    {
        /// <summary>
        /// Creates a new automation package
        /// </summary>
        /// <param name="dto">Package creation data</param>
        /// <returns>Created package response</returns>
        Task<AutomationPackageResponseDto> CreatePackageAsync(CreateAutomationPackageDto dto);

        /// <summary>
        /// Gets a package by ID
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Package response or null if not found</returns>
        Task<AutomationPackageResponseDto?> GetPackageByIdAsync(Guid id);

        /// <summary>
        /// Gets all packages for the current tenant
        /// </summary>
        /// <returns>Collection of package responses</returns>
        Task<IEnumerable<AutomationPackageResponseDto>> GetAllPackagesAsync();

        /// <summary>
        /// Uploads a new version of a package
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <param name="fileStream">File stream</param>
        /// <param name="fileName">Original filename</param>
        /// <param name="version">Version number</param>
        /// <returns>Package version response</returns>
        Task<PackageVersionResponseDto> UploadPackageVersionAsync(Guid packageId, Stream fileStream, string fileName, string version);

        /// <summary>
        /// Gets a download URL for a specific package version
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <param name="version">Version number</param>
        /// <returns>Presigned download URL</returns>
        Task<string> GetPackageDownloadUrlAsync(Guid packageId, string version);

        /// <summary>
        /// Deletes a package and all its versions
        /// </summary>
        /// <param name="id">Package ID</param>
        Task DeletePackageAsync(Guid id);

        /// <summary>
        /// Deletes a specific package version
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <param name="version">Version number</param>
        Task DeletePackageVersionAsync(Guid packageId, string version);

        /// <summary>
        /// Checks if a package with the given name and version already exists
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="version">The version number</param>
        /// <returns>True if the combination exists, false otherwise</returns>
        Task<bool> PackageVersionExistsAsync(string packageName, string version);
    }
} 