using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for initializing and managing S3 buckets
    /// </summary>
    public class S3BucketInitializer : IS3BucketInitializer, IDisposable
    {
        private readonly AmazonS3Client _s3Client;
        private readonly AwsSettings _awsSettings;
        private readonly ILogger<S3BucketInitializer> _logger;
        
        // Static dictionary to track which buckets have been initialized to avoid duplicate work
        private static readonly ConcurrentDictionary<string, bool> _initializedBuckets = new();

        /// <summary>
        /// Log message constants
        /// </summary>
        private static class LogMessages
        {
            public const string BucketCheckStarted = "Checking if S3 bucket exists: {BucketName}";
            public const string BucketExists = "S3 bucket already exists: {BucketName}";
            public const string BucketNotFound = "S3 bucket does not exist: {BucketName}";
            public const string BucketCreationStarted = "Creating S3 bucket: {BucketName} in region: {Region}";
            public const string BucketCreated = "Successfully created S3 bucket: {BucketName}";
            public const string BucketCreationFailed = "Failed to create S3 bucket: {BucketName}";
            public const string BucketAlreadyInitialized = "S3 bucket already initialized in this session: {BucketName}";
            public const string BucketInitializationCompleted = "S3 bucket initialization completed: {BucketName}";
        }

        public S3BucketInitializer(
            IOptions<AwsSettings> awsSettings,
            ILogger<S3BucketInitializer> logger)
        {
            _awsSettings = awsSettings.Value;
            _logger = logger;

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region)
            };

            _s3Client = new AmazonS3Client(_awsSettings.AccessKey, _awsSettings.SecretKey, config);
        }

        public async Task EnsureBucketExistsAsync(string bucketName, string region)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("Region cannot be null or empty", nameof(region));

            // Check if we've already initialized this bucket in this session
            var bucketKey = $"{bucketName}:{region}";
            if (_initializedBuckets.ContainsKey(bucketKey))
            {
                _logger.LogDebug(LogMessages.BucketAlreadyInitialized, bucketName);
                return;
            }

            try
            {
                _logger.LogInformation(LogMessages.BucketCheckStarted, bucketName);

                // Check if bucket exists
                if (await BucketExistsAsync(bucketName))
                {
                    _logger.LogInformation(LogMessages.BucketExists, bucketName);
                    _initializedBuckets.TryAdd(bucketKey, true);
                    return;
                }

                _logger.LogInformation(LogMessages.BucketNotFound, bucketName);
                _logger.LogInformation(LogMessages.BucketCreationStarted, bucketName, region);

                // Create the bucket
                var request = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };

                // For regions other than us-east-1, we need to specify the region
                if (!string.Equals(region, "us-east-1", StringComparison.OrdinalIgnoreCase))
                {
                    request.BucketRegion = S3Region.FindValue(region);
                }

                await _s3Client.PutBucketAsync(request);
                
                _logger.LogInformation(LogMessages.BucketCreated, bucketName);
                _initializedBuckets.TryAdd(bucketKey, true);
                
                _logger.LogInformation(LogMessages.BucketInitializationCompleted, bucketName);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Bucket already exists (race condition) - this is fine
                _logger.LogInformation("S3 bucket {BucketName} already exists (created by another process)", bucketName);
                _initializedBuckets.TryAdd(bucketKey, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.BucketCreationFailed, bucketName);
                throw new InvalidOperationException($"Failed to ensure S3 bucket exists: {bucketName}. {ex.Message}", ex);
            }
        }

        public async Task<bool> BucketExistsAsync(string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

            try
            {
                await _s3Client.GetBucketLocationAsync(bucketName);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if S3 bucket exists: {BucketName}", bucketName);
                throw new InvalidOperationException($"Failed to check if S3 bucket exists: {bucketName}. {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _s3Client?.Dispose();
        }
    }
}
