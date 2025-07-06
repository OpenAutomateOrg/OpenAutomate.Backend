using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class ExecutionTriggerServiceTests
    {
        private readonly Mock<IExecutionService> _mockExecutionService;
        private readonly Mock<IBotAgentService> _mockBotAgentService;
        private readonly Mock<IAutomationPackageService> _mockPackageService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<ExecutionTriggerService>> _mockLogger;
        private readonly Mock<Func<Guid, string, object, Task>> _mockSignalRSender;
        private readonly ExecutionTriggerService _service;

        public ExecutionTriggerServiceTests()
        {
            _mockExecutionService = new Mock<IExecutionService>();
            _mockBotAgentService = new Mock<IBotAgentService>();
            _mockPackageService = new Mock<IAutomationPackageService>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<ExecutionTriggerService>>();
            _mockSignalRSender = new Mock<Func<Guid, string, object, Task>>();

            _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(Guid.NewGuid());
            _mockTenantContext.Setup(t => t.CurrentTenantSlug).Returns("test-tenant");

            _service = new ExecutionTriggerService(
                _mockExecutionService.Object,
                _mockBotAgentService.Object,
                _mockPackageService.Object,
                _mockTenantContext.Object,
                _mockLogger.Object,
                _mockSignalRSender.Object
            );
        }

        #region TriggerExecutionAsync Tests

        [Fact]
        public async Task TriggerExecutionAsync_WithValidData_ReturnsExecutionResponse()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.TriggerExecutionAsync(triggerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal(botAgentId, result.BotAgentId);
            Assert.Equal(packageId, result.PackageId);
            Assert.Equal("Pending", result.Status);
            Assert.Equal("TestAgent", result.BotAgentName);
            Assert.Equal("TestPackage", result.PackageName);
            Assert.Equal("1.0.0", result.PackageVersion);

            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(botAgentId), Times.Once);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Once);
            _mockSignalRSender.Verify(s => s(botAgentId, "ExecutePackage", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task TriggerExecutionAsync_WithNullBotAgent_ThrowsArgumentException()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync((BotAgentResponseDto)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.TriggerExecutionAsync(triggerDto));
            
            Assert.Contains("Bot agent not found", exception.Message);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(botAgentId), Times.Once);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Never);
        }

        [Fact]
        public async Task TriggerExecutionAsync_WithDisconnectedBotAgent_ThrowsInvalidOperationException()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Disconnected"
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.TriggerExecutionAsync(triggerDto));
            
            Assert.Contains("Bot agent is disconnected", exception.Message);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(botAgentId), Times.Once);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Never);
        }

        [Fact]
        public async Task TriggerExecutionAsync_WithNullPackage_ThrowsArgumentException()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AutomationPackageResponseDto)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.TriggerExecutionAsync(triggerDto));
            
            Assert.Contains("Package not found", exception.Message);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(botAgentId), Times.Once);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Never);
        }

        [Fact]
        public async Task TriggerExecutionAsync_WithSignalRError_HandlesGracefully()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("SignalR error"));

            // Act
            var result = await _service.TriggerExecutionAsync(triggerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            _mockSignalRSender.Verify(s => s(botAgentId, "ExecutePackage", It.IsAny<object>()), Times.Once);
            // Execution should still be created and returned even if SignalR fails
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Once);
        }

        [Fact]
        public async Task TriggerExecutionAsync_WithNullSignalRSender_HandlesGracefully()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();

            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = packageId,
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            // Create a service with null SignalR sender
            var serviceWithNullSender = new ExecutionTriggerService(
                _mockExecutionService.Object,
                _mockBotAgentService.Object,
                _mockPackageService.Object,
                _mockTenantContext.Object,
                _mockLogger.Object,
                null
            );

            // Act
            var result = await serviceWithNullSender.TriggerExecutionAsync(triggerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            // Execution should still be created and returned even without SignalR sender
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Once);
        }

        #endregion

        #region TriggerScheduledExecutionAsync Tests

        [Fact]
        public async Task TriggerScheduledExecutionAsync_WithValidData_ReturnsExecutionResponse()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();
            var packageName = "TestPackage";
            var version = "1.0.0";

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = packageName,
                Versions = new List<PackageVersionResponseDto>
                {
                    new PackageVersionResponseDto { VersionNumber = version }
                }
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.TriggerScheduledExecutionAsync(
                scheduleId, botAgentId, packageId, packageName, version);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal(botAgentId, result.BotAgentId);
            Assert.Equal(packageId, result.PackageId);
            Assert.Equal("Pending", result.Status);
            Assert.Equal("TestAgent", result.BotAgentName);
            Assert.Equal(packageName, result.PackageName);
            Assert.Equal(version, result.PackageVersion);

            // TriggerScheduledExecutionAsync calls GetPackageByIdAsync and then calls TriggerExecutionAsync
            // which also calls GetPackageByIdAsync, so we expect it to be called at least once
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.AtLeastOnce);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(botAgentId), Times.Once);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Once);
            _mockSignalRSender.Verify(s => s(botAgentId, "ExecutePackage", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task TriggerScheduledExecutionAsync_WithLatestVersion_UsesFirstVersion()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();
            var packageName = "TestPackage";
            var latestVersion = "2.0.0";

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = packageName,
                Versions = new List<PackageVersionResponseDto>
                {
                    new PackageVersionResponseDto { VersionNumber = latestVersion },
                    new PackageVersionResponseDto { VersionNumber = "1.0.0" }
                }
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.TriggerScheduledExecutionAsync(
                scheduleId, botAgentId, packageId, packageName, "latest");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(latestVersion, result.PackageVersion);

            // Verify that the TriggerExecutionAsync method was called with the correct version
            _mockSignalRSender.Verify(s => s(botAgentId, "ExecutePackage", 
                It.Is<object>(o => VerifyObjectVersionProperty(o, latestVersion))), 
                Times.Once);
                
            // TriggerScheduledExecutionAsync calls GetPackageByIdAsync and then calls TriggerExecutionAsync
            // which also calls GetPackageByIdAsync, so we expect it to be called at least once
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.AtLeastOnce);
        }

        // Helper method to verify object property without using null propagation in expression tree
        private bool VerifyObjectVersionProperty(object obj, string expectedVersion)
        {
            if (obj == null) return false;
            var prop = obj.GetType().GetProperty("Version");
            if (prop == null) return false;
            var value = prop.GetValue(obj);
            if (value == null) return false;
            return value.ToString() == expectedVersion;
        }

        [Fact]
        public async Task TriggerScheduledExecutionAsync_WithEmptyVersion_UsesFirstVersion()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();
            var packageName = "TestPackage";
            var latestVersion = "2.0.0";

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = packageName,
                Versions = new List<PackageVersionResponseDto>
                {
                    new PackageVersionResponseDto { VersionNumber = latestVersion },
                    new PackageVersionResponseDto { VersionNumber = "1.0.0" }
                }
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.TriggerScheduledExecutionAsync(
                scheduleId, botAgentId, packageId, packageName, string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(latestVersion, result.PackageVersion);
            
            // TriggerScheduledExecutionAsync calls GetPackageByIdAsync and then calls TriggerExecutionAsync
            // which also calls GetPackageByIdAsync, so we expect it to be called at least once
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TriggerScheduledExecutionAsync_WithNoVersions_UsesDefaultVersion()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var executionId = Guid.NewGuid();
            var packageName = "TestPackage";
            var defaultVersion = "1.0.0";

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = packageName,
                Versions = new List<PackageVersionResponseDto>()
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow
            };

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(execution);

            _mockSignalRSender.Setup(s => s(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.TriggerScheduledExecutionAsync(
                scheduleId, botAgentId, packageId, packageName, "latest");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(defaultVersion, result.PackageVersion);
            
            // TriggerScheduledExecutionAsync calls GetPackageByIdAsync and then calls TriggerExecutionAsync
            // which also calls GetPackageByIdAsync, so we expect it to be called at least once
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TriggerScheduledExecutionAsync_WithNullPackage_ThrowsArgumentException()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var packageName = "TestPackage";
            var version = "1.0.0";

            _mockPackageService.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AutomationPackageResponseDto)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.TriggerScheduledExecutionAsync(
                    scheduleId, botAgentId, packageId, packageName, version));
            
            Assert.Contains("Package not found", exception.Message);
            _mockPackageService.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()), Times.Never);
        }

        #endregion
    }
} 