using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class AssetTest
    {
        [Fact]
        public void Asset_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var asset = new Asset();

            // Assert
            Assert.NotNull(asset);
            Assert.Equal(string.Empty, asset.Key);
            Assert.Equal(string.Empty, asset.Value);
            Assert.Equal(string.Empty, asset.Description);
            Assert.True(asset.IsEncrypted);
            Assert.Equal(Guid.Empty, asset.OrganizationUnitId);
            Assert.NotNull(asset.AssetBotAgents);
            Assert.Empty(asset.AssetBotAgents);
        }
        [Fact]
        public void Asset_SetName_NameIsSet()
        {
            // Arrange
            var asset = new Asset();
            var name = "Test Asset";

            // Act


            // Assert

        }
        [Fact]
        public void Asset_SetKey_KeyIsSet()
        {
            // Arrange
            var asset = new Asset();
            var key = "test-key";

            // Act
            asset.Key = key;

            // Assert
            Assert.Equal(key, asset.Key);
        }
        [Fact]
        public void Asset_SetValue_ValueIsSet()
        {
            // Arrange
            var asset = new Asset();
            var value = "test-value";

            // Act
            asset.Value = value;

            // Assert
            Assert.Equal(value, asset.Value);
        }
        [Fact]
        public void Asset_LinkOrganizationUnit_OrganizationUnitIsLinked()
        {
            // Arrange
            var orgUnit = new OrganizationUnit { Name = "Test Organization", Description = "Test Description" };
            var asset = new Asset { OrganizationUnit = orgUnit };

            // Act
            var linkedOrgUnit = asset.OrganizationUnit;

            // Assert
            Assert.NotNull(linkedOrgUnit);
            Assert.Equal("Test Organization", linkedOrgUnit.Name);
            Assert.Equal("Test Description", linkedOrgUnit.Description);
        }
        [Fact]
        public void Asset_AddAssetBotAgent_AssetBotAgentIsAdded()
        {
            // Arrange
            var asset = new Asset();
            var assetBotAgent = new AssetBotAgent { AssetId = Guid.NewGuid(), BotAgentId = Guid.NewGuid() };

            // Act
            asset.AssetBotAgents.Add(assetBotAgent);

            // Assert
            Assert.NotNull(asset.AssetBotAgents);
            Assert.Contains(assetBotAgent, asset.AssetBotAgents);
            Assert.Single(asset.AssetBotAgents);
        }
        [Fact]
        public void Asset_SetIsEncrypted_IsEncryptedIsSet()
        {
            // Arrange
            var asset = new Asset();

            // Act
            asset.IsEncrypted = false;

            // Assert
            Assert.False(asset.IsEncrypted);
        }

    }
}
