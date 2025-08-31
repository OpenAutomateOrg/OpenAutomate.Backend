using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Infrastructure.Services;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class S3BucketInitializerTests : IDisposable
    {
        private readonly Mock<ILogger<S3BucketInitializer>> _mockLogger;
        private readonly IOptions<AwsSettings> _awsOptions;
        private readonly S3BucketInitializer _service;

        public S3BucketInitializerTests()
        {
            _mockLogger = new Mock<ILogger<S3BucketInitializer>>();
            _awsOptions = Options.Create(new AwsSettings
            {
                Region = "us-east-1",
                BucketName = "test-bucket",
                AccessKey = "test-access-key",
                SecretKey = "test-secret-key",
                PresignedUrlExpirationMinutes = 15
            });

            _service = new S3BucketInitializer(_awsOptions, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var service = new S3BucketInitializer(_awsOptions, _mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_WithNullBucketName_ShouldThrowArgumentException()
        {
            // Arrange
            string bucketName = null!;
            string region = "us-east-1";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.EnsureBucketExistsAsync(bucketName, region));
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_WithEmptyBucketName_ShouldThrowArgumentException()
        {
            // Arrange
            string bucketName = "";
            string region = "us-east-1";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.EnsureBucketExistsAsync(bucketName, region));
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_WithNullRegion_ShouldThrowArgumentException()
        {
            // Arrange
            string bucketName = "test-bucket";
            string region = null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.EnsureBucketExistsAsync(bucketName, region));
        }

        [Fact]
        public async Task BucketExistsAsync_WithNullBucketName_ShouldThrowArgumentException()
        {
            // Arrange
            string bucketName = null!;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.BucketExistsAsync(bucketName));
        }

        [Fact]
        public async Task BucketExistsAsync_WithEmptyBucketName_ShouldThrowArgumentException()
        {
            // Arrange
            string bucketName = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.BucketExistsAsync(bucketName));
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }
}
