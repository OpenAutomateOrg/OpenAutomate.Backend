namespace OpenAutomate.Core.Configurations
{
    /// <summary>
    /// AWS configuration settings for S3 storage
    /// </summary>
    public class AwsSettings
    {
        /// <summary>
        /// AWS region for S3 bucket
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// S3 bucket name for package storage
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// AWS access key for S3 operations
        /// </summary>
        public string AccessKey { get; set; } = string.Empty;

        /// <summary>
        /// AWS secret key for S3 operations
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Presigned URL expiration time in minutes
        /// </summary>
        public int PresignedUrlExpirationMinutes { get; set; } = 15;
    }
} 