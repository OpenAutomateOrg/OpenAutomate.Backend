using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class AutomationPackageTest
    {
        [Fact]
        public void AutomationPackage_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var package = new AutomationPackage();

            // Assert
            Assert.NotNull(package);

            // Check string properties
            Assert.Equal(string.Empty, package.Name);
            Assert.Equal(string.Empty, package.Description);

            // Check boolean properties
            Assert.True(package.IsActive);

            // Check nullable reference types
            Assert.Null(package.Creator);

            // Check collection properties
            Assert.NotNull(package.Versions);
            Assert.Empty(package.Versions);
            Assert.NotNull(package.Executions);
            Assert.Empty(package.Executions);

           
        }
        [Fact]
        public void AutomationPackage_SetName_NameIsSet()
        {
            // Arrange
            var package = new AutomationPackage();
            var name = "Test Package";

            // Act
            package.Name = name;

            // Assert
            Assert.Equal(name, package.Name);
        }
        [Fact]
        public void AutomationPackage_SetDescription_DescriptionIsSet()
        {
            // Arrange
            var package = new AutomationPackage();
            var description = "This is a test package.";

            // Act
            package.Description = description;

            // Assert
            Assert.Equal(description, package.Description);
        }
        [Fact]
        public void AutomationPackage_SetIsActive_IsActiveIsSet()
        {
            // Arrange
            var package = new AutomationPackage();

            // Act
            package.IsActive = false;

            // Assert
            Assert.False(package.IsActive);
        }
        [Fact]
        public void AutomationPackage_LinkCreator_CreatorIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var package = new AutomationPackage { Creator = user };

            // Act
            var linkedCreator = package.Creator;

            // Assert
            Assert.NotNull(linkedCreator);
            Assert.Equal("John", linkedCreator.FirstName);
            Assert.Equal("Doe", linkedCreator.LastName);
        }
        [Fact]
        public void AutomationPackage_AddPackageVersion_VersionIsAdded()
        {
            // Arrange
            var package = new AutomationPackage { Versions = new List<PackageVersion>() };
            var version = new PackageVersion { VersionNumber = "1.0.0" };

            // Act
            package.Versions.Add(version);

            // Assert
            Assert.NotNull(package.Versions);
            Assert.Contains(version, package.Versions);
            Assert.Single(package.Versions);
        }
        [Fact]
        public void AutomationPackage_AddExecution_ExecutionIsAdded()
        {
            // Arrange
            var package = new AutomationPackage { Executions = new List<Execution>() };
            var execution = new Execution { Status = "Completed" };

            // Act
            package.Executions.Add(execution);

            // Assert
            Assert.NotNull(package.Executions);
            Assert.Contains(execution, package.Executions);
            Assert.Single(package.Executions);
        }


    }
}
