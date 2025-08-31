using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// S3 implementation of log storage service
    /// </summary>
    public class S3LogStorageService : ILogStorageService, IDisposable
    {
        private readonly AmazonS3Client _s3Client;
        private readonly AwsSettings _awsSettings;
        private readonly ILogger<S3LogStorageService> _logger;
        private const string LogsPrefix = "logs/";

        // Standardized log message templates
        private static class LogMessages
        {
            public const string LogUploadStarted = "Starting log upload to S3: {ObjectKey}";
            public const string LogUploadCompleted = "Successfully uploaded log to S3: {ObjectKey}";
            public const string LogUploadFailed = "Failed to upload log to S3: {ObjectKey}";
            public const string LogDownloadUrlGenerated = "Generated presigned URL for log: {ObjectKey}";
            public const string LogDownloadUrlFailed = "Failed to generate presigned URL for log: {ObjectKey}";
            public const string LogDeleteCompleted = "Successfully deleted log from S3: {ObjectKey}";
            public const string LogDeleteFailed = "Failed to delete log from S3: {ObjectKey}";
            public const string LogExistsCheck = "Checking if log exists in S3: {ObjectKey}";
            public const string LogExistsCheckFailed = "Failed to check if log exists in S3: {ObjectKey}";
        }

        public S3LogStorageService(
            IOptions<AwsSettings> awsSettings,
            ILogger<S3LogStorageService> logger,
            IS3BucketInitializer bucketInitializer)
        {
            _awsSettings = awsSettings.Value;
            _logger = logger;

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region)
            };

            _s3Client = new AmazonS3Client(_awsSettings.AccessKey, _awsSettings.SecretKey, config);

            // Ensure the S3 bucket exists
            _ = Task.Run(async () =>
            {
                try
                {
                    await bucketInitializer.EnsureBucketExistsAsync(_awsSettings.BucketName, _awsSettings.Region);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize S3 bucket during service construction: {BucketName}", _awsSettings.BucketName);
                    // Don't throw here to avoid breaking service registration, but log the error
                }
            });
        }

        public async Task<string> UploadLogAsync(Stream logStream, string objectKey, string contentType = "text/plain")
        {
            try
            {
                // Ensure the object key has the logs prefix
                var fullObjectKey = objectKey.StartsWith(LogsPrefix) ? objectKey : $"{LogsPrefix}{objectKey}";
                
                _logger.LogInformation(LogMessages.LogUploadStarted, fullObjectKey);

                var request = new PutObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = fullObjectKey,
                    InputStream = logStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                _logger.LogInformation(LogMessages.LogUploadCompleted, fullObjectKey);
                return fullObjectKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogUploadFailed, objectKey);
                throw new InvalidOperationException($"Failed to upload log: {ex.Message}", ex);
            }
        }

        public async Task<string> GetLogDownloadUrlAsync(string objectKey, TimeSpan expiresIn)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = objectKey,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.Add(expiresIn)
                };

                var url = await _s3Client.GetPreSignedURLAsync(request);
                
                _logger.LogInformation(LogMessages.LogDownloadUrlGenerated, objectKey);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogDownloadUrlFailed, objectKey);
                throw new InvalidOperationException($"Failed to generate download URL: {ex.Message}", ex);
            }
        }

        public async Task DeleteLogAsync(string objectKey)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = objectKey
                };

                await _s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation(LogMessages.LogDeleteCompleted, objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogDeleteFailed, objectKey);
                throw new InvalidOperationException($"Failed to delete log: {ex.Message}", ex);
            }
        }

        public async Task<bool> LogExistsAsync(string objectKey)
        {
            try
            {
                _logger.LogDebug(LogMessages.LogExistsCheck, objectKey);

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = objectKey
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.LogExistsCheckFailed, objectKey);
                throw new InvalidOperationException($"Failed to check log existence: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _s3Client?.Dispose();
        }
    }
} 