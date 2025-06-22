using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class ExecutionTest
    {
        [Fact]
        public void Execution_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var execution = new Execution();

            // Assert
            Assert.NotNull(execution);
            Assert.Equal(Guid.Empty, execution.BotAgentId);
            Assert.Equal(Guid.Empty, execution.PackageId);
            Assert.Equal(string.Empty, execution.Status);
            Assert.Equal(DateTime.MinValue, execution.StartTime);
            Assert.Null(execution.EndTime);
            Assert.Null(execution.LogOutput);
            Assert.Null(execution.ErrorMessage);
            Assert.Null(execution.BotAgent);
            Assert.Null(execution.Package);
        }
        [Fact]
        public void Execution_SetStatus_StatusIsSet()
        {
            // Arrange
            var execution = new Execution();
            var status = "Completed";

            // Act
            execution.Status = status;

            // Assert
            Assert.Equal(status, execution.Status);
        }
        [Fact]
        public void Execution_SetStartTimeAndEndTime_TimesAreSet()
        {
            // Arrange
            var execution = new Execution();
            var startTime = DateTime.UtcNow;
            var endTime = DateTime.UtcNow.AddHours(1);

            // Act
            execution.StartTime = startTime;
            execution.EndTime = endTime;

            // Assert
            Assert.Equal(startTime, execution.StartTime);
            Assert.Equal(endTime, execution.EndTime);
        }
        [Fact]
        public void Execution_LinkBotAgent_BotAgentIsLinked()
        {
            // Arrange
            var botAgent = new BotAgent { Name = "Test Bot", MachineKey = "secure-key" };
            var execution = new Execution { BotAgent = botAgent };

            // Act
            var linkedBotAgent = execution.BotAgent;

            // Assert
            Assert.NotNull(linkedBotAgent);
            Assert.Equal("Test Bot", linkedBotAgent.Name);
            Assert.Equal("secure-key", linkedBotAgent.MachineKey);
        }
        [Fact]
        public void Execution_LinkAutomationPackage_PackageIsLinked()
        {
            // Arrange
            var package = new AutomationPackage { Name = "Test Package", Description = "Test Description" };
            var execution = new Execution { Package = package };

            // Act
            var linkedPackage = execution.Package;

            // Assert
            Assert.NotNull(linkedPackage);
            Assert.Equal("Test Package", linkedPackage.Name);
            Assert.Equal("Test Description", linkedPackage.Description);
        }

        [Fact]
        public void Execution_SetLogOutputAndErrorMessage_ValuesAreSet()
        {
            // Arrange
            var execution = new Execution();
            var logOutput = "Execution completed successfully.";
            var errorMessage = "No errors.";

            // Act
            execution.LogOutput = logOutput;
            execution.ErrorMessage = errorMessage;

            // Assert
            Assert.Equal(logOutput, execution.LogOutput);
            Assert.Equal(errorMessage, execution.ErrorMessage);
        }

    }
}
