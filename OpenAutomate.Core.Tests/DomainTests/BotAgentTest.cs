using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class BotAgentTest
    {
        [Fact]
        public void BotAgent_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var botAgent = new BotAgent();

            // Assert
            Assert.NotNull(botAgent);
            Assert.Equal(string.Empty, botAgent.Name);
            Assert.Equal(string.Empty, botAgent.MachineKey);
            Assert.Equal(string.Empty, botAgent.MachineName);
            Assert.Equal(string.Empty, botAgent.Status);
            Assert.Equal(DateTime.MinValue, botAgent.LastConnected);
            Assert.Equal(DateTime.MinValue, botAgent.LastHeartbeat);
            Assert.True(botAgent.IsActive);
            Assert.Null(botAgent.Owner);
            Assert.Null(botAgent.Executions);
            Assert.Null(botAgent.AssetBotAgents);
        }
        [Fact]
        public void BotAgent_SetName_NameIsSet()
        {
            // Arrange
            var botAgent = new BotAgent();
            var name = "Test Bot";

            // Act
            botAgent.Name = name;

            // Assert
            Assert.Equal(name, botAgent.Name);
        }
        [Fact]
        public void BotAgent_SetMachineKey_MachineKeyIsSet()
        {
            // Arrange
            var botAgent = new BotAgent();
            var machineKey = "secure-key";

            // Act
            botAgent.MachineKey = machineKey;

            // Assert
            Assert.Equal(machineKey, botAgent.MachineKey);
        }
        [Fact]
        public void BotAgent_LinkOwner_OwnerIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var botAgent = new BotAgent { Owner = user };

            // Act
            var linkedOwner = botAgent.Owner;

            // Assert
            Assert.NotNull(linkedOwner);
            Assert.Equal("John", linkedOwner.FirstName);
            Assert.Equal("Doe", linkedOwner.LastName);
        }
        [Fact]
        public void BotAgent_AddExecution_ExecutionIsAdded()
        {
            // Arrange
            var botAgent = new BotAgent { Executions = new List<Execution>() };
            var execution = new Execution { Status = "Completed", StartTime = DateTime.UtcNow };

            // Act
            botAgent.Executions.Add(execution);

            // Assert
            Assert.NotNull(botAgent.Executions);
            Assert.Contains(execution, botAgent.Executions);
            Assert.Single(botAgent.Executions);
        }
        [Fact]
        public void BotAgent_AddAssetBotAgent_AssetBotAgentIsAdded()
        {
            // Arrange
            var botAgent = new BotAgent { AssetBotAgents = new List<AssetBotAgent>() };
            var assetBotAgent = new AssetBotAgent { AssetId = Guid.NewGuid(), BotAgentId = Guid.NewGuid() };

            // Act
            botAgent.AssetBotAgents.Add(assetBotAgent);

            // Assert
            Assert.NotNull(botAgent.AssetBotAgents);
            Assert.Contains(assetBotAgent, botAgent.AssetBotAgents);
            Assert.Single(botAgent.AssetBotAgents);
        }
        [Fact]
        public void BotAgent_SetIsActive_IsActiveIsSet()
        {
            // Arrange
            var botAgent = new BotAgent();

            // Act
            botAgent.IsActive = false;

            // Assert
            Assert.False(botAgent.IsActive);
        }

    }
}


