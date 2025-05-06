using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class PackageVersionTest
    {
        [Fact]
        public void PackageVersion_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var packageVersion = new PackageVersion();

            // Assert
            Assert.NotNull(packageVersion);
            Assert.Equal(string.Empty, packageVersion.VersionNumber);
            Assert.Equal(string.Empty, packageVersion.FilePath);
            Assert.False(packageVersion.IsActive);
            Assert.Equal(Guid.Empty, packageVersion.PackageId);
            Assert.Null(packageVersion.Package);
        }
        [Fact]
        public void PackageVersion_SetVersionNumber_VersionNumberIsSet()
        {
            // Arrange
            var packageVersion = new PackageVersion();
            var versionNumber = "1.0.0";

            // Act
            packageVersion.VersionNumber = versionNumber;

            // Assert
            Assert.Equal(versionNumber, packageVersion.VersionNumber);
        }
        [Fact]
        public void PackageVersion_SetFilePath_FilePathIsSet()
        {
            // Arrange
            var packageVersion = new PackageVersion();
            var filePath = "/path/to/package.zip";

            // Act
            packageVersion.FilePath = filePath;

            // Assert
            Assert.Equal(filePath, packageVersion.FilePath);
        }
        [Fact]
        public void PackageVersion_SetIsActive_IsActiveIsSet()
        {
            // Arrange
            var packageVersion = new PackageVersion();

            // Act
            packageVersion.IsActive = true;

            // Assert
            Assert.True(packageVersion.IsActive);
        }
        [Fact]
        public void PackageVersion_LinkAutomationPackage_PackageIsLinked()
        {
            // Arrange
            var package = new AutomationPackage { Name = "Test Package", Description = "Test Description" };
            var packageVersion = new PackageVersion { Package = package };

            // Act
            var linkedPackage = packageVersion.Package;

            // Assert
            Assert.NotNull(linkedPackage);
            Assert.Equal("Test Package", linkedPackage.Name);
            Assert.Equal("Test Description", linkedPackage.Description);
        }
        [Fact]
        public void PackageVersion_SetPackageId_PackageIdIsSet()
        {
            // Arrange
            var packageVersion = new PackageVersion();
            var packageId = Guid.NewGuid();

            // Act
            packageVersion.PackageId = packageId;

            // Assert
            Assert.Equal(packageId, packageVersion.PackageId);
        }

    }
}
