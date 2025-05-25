using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for package storage operations
    /// </summary>
    public interface IPackageStorageService
    {
        /// <summary>
        /// Uploads a package file to storage
        /// </summary>
        /// <param name="packageStream">The package file stream</param>
        /// <param name="objectKey">The unique object key for the file</param>
        /// <param name="contentType">The content type of the file</param>
        /// <returns>The storage path/key of the uploaded file</returns>
        Task<string> UploadAsync(Stream packageStream, string objectKey, string contentType = "application/zip");

        /// <summary>
        /// Generates a presigned download URL for a package
        /// </summary>
        /// <param name="objectKey">The object key of the file</param>
        /// <param name="expiresIn">How long the URL should be valid</param>
        /// <returns>A presigned URL for downloading the file</returns>
        Task<string> GetDownloadUrlAsync(string objectKey, TimeSpan expiresIn);

        /// <summary>
        /// Deletes a package file from storage
        /// </summary>
        /// <param name="objectKey">The object key of the file to delete</param>
        Task DeleteAsync(string objectKey);

        /// <summary>
        /// Checks if a file exists in storage
        /// </summary>
        /// <param name="objectKey">The object key to check</param>
        /// <returns>True if the file exists</returns>
        Task<bool> ExistsAsync(string objectKey);
    }
} 