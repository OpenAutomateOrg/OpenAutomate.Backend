using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Interface for log storage operations
    /// </summary>
    public interface ILogStorageService
    {
        /// <summary>
        /// Uploads a log file to S3 storage
        /// </summary>
        /// <param name="logStream">Stream containing the log file data</param>
        /// <param name="objectKey">S3 object key for the log file</param>
        /// <param name="contentType">Content type of the log file</param>
        /// <returns>The S3 object key of the uploaded log file</returns>
        Task<string> UploadLogAsync(Stream logStream, string objectKey, string contentType = "text/plain");

        /// <summary>
        /// Generates a pre-signed URL for downloading a log file
        /// </summary>
        /// <param name="objectKey">S3 object key of the log file</param>
        /// <param name="expiresIn">How long the URL should be valid</param>
        /// <returns>Pre-signed download URL</returns>
        Task<string> GetLogDownloadUrlAsync(string objectKey, TimeSpan expiresIn);

        /// <summary>
        /// Deletes a log file from S3 storage
        /// </summary>
        /// <param name="objectKey">S3 object key of the log file to delete</param>
        Task DeleteLogAsync(string objectKey);

        /// <summary>
        /// Checks if a log file exists in S3 storage
        /// </summary>
        /// <param name="objectKey">S3 object key to check</param>
        /// <returns>True if the log file exists, false otherwise</returns>
        Task<bool> LogExistsAsync(string objectKey);
    }
} 