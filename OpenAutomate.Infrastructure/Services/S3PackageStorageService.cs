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
    /// S3 implementation of package storage service
    /// </summary>
    public class S3PackageStorageService : IPackageStorageService, IDisposable
    {
        private readonly AmazonS3Client _s3Client;
        private readonly AwsSettings _awsSettings;
        private readonly ILogger<S3PackageStorageService> _logger;

        public S3PackageStorageService(
            IOptions<AwsSettings> awsSettings,
            ILogger<S3PackageStorageService> logger)
        {
            _awsSettings = awsSettings.Value;
            _logger = logger;

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region)
            };

            _s3Client = new AmazonS3Client(_awsSettings.AccessKey, _awsSettings.SecretKey, config);
        }

        public async Task<string> UploadAsync(Stream packageStream, string objectKey, string contentType = "application/zip")
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = objectKey,
                    InputStream = packageStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                _logger.LogInformation("Successfully uploaded package to S3: {ObjectKey}", objectKey);
                return objectKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload package to S3: {ObjectKey}", objectKey);
                throw new InvalidOperationException($"Failed to upload package: {ex.Message}", ex);
            }
        }

        public async Task<string> GetDownloadUrlAsync(string objectKey, TimeSpan expiresIn)
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
                
                _logger.LogInformation("Generated presigned URL for package: {ObjectKey}", objectKey);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate presigned URL for package: {ObjectKey}", objectKey);
                throw new InvalidOperationException($"Failed to generate download URL: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(string objectKey)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = objectKey
                };

                await _s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation("Successfully deleted package from S3: {ObjectKey}", objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete package from S3: {ObjectKey}", objectKey);
                throw new InvalidOperationException($"Failed to delete package: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(string objectKey)
        {
            try
            {
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
                _logger.LogError(ex, "Failed to check if package exists in S3: {ObjectKey}", objectKey);
                throw new InvalidOperationException($"Failed to check package existence: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _s3Client?.Dispose();
        }
    }
} 