using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class AssetBotAgentTest
    {
        [Fact]
        public void AssetBotAgent_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var assetBotAgent = new AssetBotAgent();

            // Assert
            Assert.NotNull(assetBotAgent);
            Assert.Equal(Guid.Empty, assetBotAgent.AssetId);
            Assert.Equal(Guid.Empty, assetBotAgent.BotAgentId);
            Assert.Null(assetBotAgent.Asset);
            Assert.Null(assetBotAgent.BotAgent);
        }
        [Fact]
        public void AssetBotAgent_LinkAsset_AssetIsLinked()
        {
            // Arrange
            var asset = new Asset { Key = "test-key", Value = "test-value" };
            var assetBotAgent = new AssetBotAgent { Asset = asset };

            // Act
            var linkedAsset = assetBotAgent.Asset;

            // Assert
            Assert.NotNull(linkedAsset);
            Assert.Equal("test-key", linkedAsset.Key);
            Assert.Equal("test-value", linkedAsset.Value);
        }
        [Fact]
        public void AssetBotAgent_LinkBotAgent_BotAgentIsLinked()
        {
            // Arrange
            var botAgent = new BotAgent { Name = "Test Bot", MachineKey = "machine-key", MachineName = "machine-name" };
            var assetBotAgent = new AssetBotAgent { BotAgent = botAgent };

            // Act
            var linkedBotAgent = assetBotAgent.BotAgent;

            // Assert
            Assert.NotNull(linkedBotAgent);
            Assert.Equal("Test Bot", linkedBotAgent.Name);
            Assert.Equal("machine-key", linkedBotAgent.MachineKey);
            Assert.Equal("machine-name", linkedBotAgent.MachineName);
        }
        [Fact]
        public void AssetBotAgent_SetIds_IdsAreSetCorrectly()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var assetBotAgent = new AssetBotAgent
            {
                AssetId = assetId,
                BotAgentId = botAgentId
            };

            // Act & Assert
            Assert.Equal(assetId, assetBotAgent.AssetId);
            Assert.Equal(botAgentId, assetBotAgent.BotAgentId);
        }

    }
}
