using OpenAutomate.Core.Utilities;
using Xunit;

namespace OpenAutomate.Core.Tests.Utilities
{
    public class CacheKeyUtilityTests
    {
        [Fact]
        public void GenerateApiResponseKey_Should_Create_Predictable_Prefix_With_Hash_Suffix()
        {
            // Arrange
            var method = "GET";
            var path = "/odata/Assets";
            var queryString = "$filter=contains(Key,'test')&$orderby=Name asc";
            var tenantId = "tenant-123";

            // Act
            var cacheKey = CacheKeyUtility.GenerateApiResponseKey(method, path, queryString, tenantId);

            // Assert
            var expectedPrefix = "api-cache:tenant-123:/odata/Assets:";
            Assert.StartsWith(expectedPrefix, cacheKey);
            Assert.True(cacheKey.Length > expectedPrefix.Length); // Should have hash after prefix
        }

        [Fact]
        public void GenerateApiResponseKey_Should_Create_Different_Hashes_For_Different_Queries()
        {
            // Arrange
            var method = "GET";
            var path = "/odata/Assets";
            var tenantId = "tenant-123";

            // Act
            var key1 = CacheKeyUtility.GenerateApiResponseKey(method, path, "$filter=contains(Key,'test')", tenantId);
            var key2 = CacheKeyUtility.GenerateApiResponseKey(method, path, "$top=20&$skip=0", tenantId);

            // Assert
            var expectedPrefix = "api-cache:tenant-123:/odata/Assets:";
            Assert.StartsWith(expectedPrefix, key1);
            Assert.StartsWith(expectedPrefix, key2);
            Assert.NotEqual(key1, key2); // Different queries should produce different hashes
        }

        [Fact]
        public void GenerateApiResponseKey_Should_Create_Different_Keys_For_Different_Tenants()
        {
            // Arrange
            var method = "GET";
            var path = "/odata/Assets";
            var queryString = "$filter=contains(Key,'test')";

            // Act
            var key1 = CacheKeyUtility.GenerateApiResponseKey(method, path, queryString, "tenant-123");
            var key2 = CacheKeyUtility.GenerateApiResponseKey(method, path, queryString, "tenant-456");

            // Assert
            Assert.StartsWith("api-cache:tenant-123:/odata/Assets:", key1);
            Assert.StartsWith("api-cache:tenant-456:/odata/Assets:", key2);
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GenerateApiResponsePattern_Should_Create_Correct_Wildcard_Pattern()
        {
            // Arrange
            var basePath = "/odata/Assets";
            var tenantId = "tenant-123";

            // Act
            var pattern = CacheKeyUtility.GenerateApiResponsePattern(basePath, tenantId);

            // Assert
            Assert.Equal("api-cache:tenant-123:/odata/Assets:*", pattern);
        }

        [Fact]
        public void GenerateApiResponsePattern_Should_Handle_Path_With_Query_Parameters()
        {
            // Arrange
            var pathWithQuery = "/odata/Assets?$filter=test";
            var tenantId = "tenant-123";

            // Act
            var pattern = CacheKeyUtility.GenerateApiResponsePattern(pathWithQuery, tenantId);

            // Assert
            // Should strip query parameters and only use base path
            Assert.Equal("api-cache:tenant-123:/odata/Assets:*", pattern);
        }

        [Fact]
        public void GenerateApiResponsePattern_Should_Handle_Global_Tenant()
        {
            // Arrange
            var basePath = "/odata/Assets";

            // Act
            var pattern = CacheKeyUtility.GenerateApiResponsePattern(basePath, null);

            // Assert
            Assert.Equal("api-cache:global:/odata/Assets:*", pattern);
        }

        [Fact]
        public void Cache_Key_And_Pattern_Should_Be_Compatible()
        {
            // Arrange
            var method = "GET";
            var path = "/odata/Assets";
            var queryString = "$filter=contains(Key,'test')&$orderby=Name asc";
            var tenantId = "tenant-123";

            // Act
            var cacheKey = CacheKeyUtility.GenerateApiResponseKey(method, path, queryString, tenantId);
            var pattern = CacheKeyUtility.GenerateApiResponsePattern(path, tenantId);

            // Assert
            // The cache key should match the pattern (if we replace * with anything)
            var patternBase = pattern.Replace(":*", ":");
            Assert.StartsWith(patternBase, cacheKey);
        }
    }
}