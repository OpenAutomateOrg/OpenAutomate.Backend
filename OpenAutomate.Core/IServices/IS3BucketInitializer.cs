using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service interface for S3 bucket initialization and management
    /// </summary>
    public interface IS3BucketInitializer
    {
        /// <summary>
        /// Ensures that the specified S3 bucket exists, creating it if necessary
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket to ensure exists</param>
        /// <param name="region">The AWS region where the bucket should be created</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task EnsureBucketExistsAsync(string bucketName, string region);

        /// <summary>
        /// Checks if the specified S3 bucket exists
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket to check</param>
        /// <returns>True if the bucket exists, false otherwise</returns>
        Task<bool> BucketExistsAsync(string bucketName);
    }
}
