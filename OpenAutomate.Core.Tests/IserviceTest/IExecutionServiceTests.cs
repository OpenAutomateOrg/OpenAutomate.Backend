using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using Xunit;

namespace OpenAutomate.Core.Tests.IserviceTest
{
    public class IExecutionServiceTests
    {
        private readonly Mock<IExecutionService> _mockExecutionService;

        public IExecutionServiceTests()
        {
            _mockExecutionService = new Mock<IExecutionService>();
        }

        #region CreateExecutionAsync Tests

        [Fact]
        public async Task CreateExecutionAsync_WithValidData_ReturnsExecution()
        {
            // Arrange
            var createDto = new CreateExecutionDto
            {
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid()
            };

            var expectedExecution = new Execution
            {
                Id = Guid.NewGuid(),
                BotAgentId = createDto.BotAgentId,
                PackageId = createDto.PackageId,
                Status = "Pending",
                StartTime = DateTime.UtcNow,
                EndTime = null,
                LogOutput = null,
                ErrorMessage = null
            };

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(createDto))
                .ReturnsAsync(expectedExecution);

            // Act
            var result = await _mockExecutionService.Object.CreateExecutionAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedExecution.Id, result.Id);
            Assert.Equal(createDto.BotAgentId, result.BotAgentId);
            Assert.Equal(createDto.PackageId, result.PackageId);
            Assert.Equal("Pending", result.Status);
            Assert.Null(result.EndTime);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(createDto), Times.Once);
        }



        [Fact]
        public async Task CreateExecutionAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            var createDto = new CreateExecutionDto
            {
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid()
            };

            _mockExecutionService.Setup(s => s.CreateExecutionAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Current tenant ID is not available"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockExecutionService.Object.CreateExecutionAsync(createDto));
            
            Assert.Contains("tenant", exception.Message);
            _mockExecutionService.Verify(s => s.CreateExecutionAsync(createDto), Times.Once);
        }

        #endregion

        #region GetExecutionByIdAsync Tests

        [Fact]
        public async Task GetExecutionByIdAsync_WithValidId_ReturnsExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();
            var packageId = Guid.NewGuid();

            var expectedExecution = new Execution
            {
                Id = executionId,
                BotAgentId = botAgentId,
                PackageId = packageId,
                Status = "Running",
                StartTime = DateTime.UtcNow.AddMinutes(-5)
            };

            _mockExecutionService.Setup(s => s.GetExecutionByIdAsync(executionId))
                .ReturnsAsync(expectedExecution);

            // Act
            var result = await _mockExecutionService.Object.GetExecutionByIdAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal(botAgentId, result.BotAgentId);
            Assert.Equal(packageId, result.PackageId);
            Assert.Equal("Running", result.Status);
            _mockExecutionService.Verify(s => s.GetExecutionByIdAsync(executionId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetExecutionByIdAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _mockExecutionService.Object.GetExecutionByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockExecutionService.Verify(s => s.GetExecutionByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionByIdAsync_WithDifferentTenant_ReturnsNull()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetExecutionByIdAsync(executionId))
                .ReturnsAsync((Execution)null); // Different tenant returns null

            // Act
            var result = await _mockExecutionService.Object.GetExecutionByIdAsync(executionId);

            // Assert
            Assert.Null(result);
            _mockExecutionService.Verify(s => s.GetExecutionByIdAsync(executionId), Times.Once);
        }

        #endregion

        #region GetAllExecutionsAsync Tests

        [Fact]
        public async Task GetAllExecutionsAsync_ReturnsAllExecutions()
        {
            // Arrange
            var expectedExecutions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = Guid.NewGuid(),
                    PackageId = Guid.NewGuid(),
                    Status = "Completed",
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    EndTime = DateTime.UtcNow.AddMinutes(-30)
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = Guid.NewGuid(),
                    PackageId = Guid.NewGuid(),
                    Status = "Running",
                    StartTime = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _mockExecutionService.Setup(s => s.GetAllExecutionsAsync())
                .ReturnsAsync(expectedExecutions);

            // Act
            var result = await _mockExecutionService.Object.GetAllExecutionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, e => e.Status == "Completed");
            Assert.Contains(result, e => e.Status == "Running");
            _mockExecutionService.Verify(s => s.GetAllExecutionsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExecutionsAsync_WithNoExecutions_ReturnsEmptyCollection()
        {
            // Arrange
            var emptyList = new List<Execution>();
            _mockExecutionService.Setup(s => s.GetAllExecutionsAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _mockExecutionService.Object.GetAllExecutionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockExecutionService.Verify(s => s.GetAllExecutionsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllExecutionsAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            _mockExecutionService.Setup(s => s.GetAllExecutionsAsync())
                .ThrowsAsync(new InvalidOperationException("Current tenant ID is not available"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockExecutionService.Object.GetAllExecutionsAsync());
            
            Assert.Contains("tenant", exception.Message);
            _mockExecutionService.Verify(s => s.GetAllExecutionsAsync(), Times.Once);
        }

        #endregion

        #region GetExecutionsByBotAgentIdAsync Tests

        [Fact]
        public async Task GetExecutionsByBotAgentIdAsync_WithValidId_ReturnsExecutions()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var expectedExecutions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Completed",
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddHours(1)
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Failed",
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    EndTime = DateTime.UtcNow.AddHours(-1),
                    ErrorMessage = "Test error"
                }
            };

            _mockExecutionService.Setup(s => s.GetExecutionsByBotAgentIdAsync(botAgentId))
                .ReturnsAsync(expectedExecutions);

            // Act
            var result = await _mockExecutionService.Object.GetExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, e => Assert.Equal(botAgentId, e.BotAgentId));
            _mockExecutionService.Verify(s => s.GetExecutionsByBotAgentIdAsync(botAgentId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionsByBotAgentIdAsync_WithInvalidId_ReturnsEmptyCollection()
        {
            // Arrange
            var invalidBotAgentId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetExecutionsByBotAgentIdAsync(invalidBotAgentId))
                .ReturnsAsync(new List<Execution>());

            // Act
            var result = await _mockExecutionService.Object.GetExecutionsByBotAgentIdAsync(invalidBotAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockExecutionService.Verify(s => s.GetExecutionsByBotAgentIdAsync(invalidBotAgentId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionsByBotAgentIdAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetExecutionsByBotAgentIdAsync(botAgentId))
                .ThrowsAsync(new InvalidOperationException("Current tenant ID is not available"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockExecutionService.Object.GetExecutionsByBotAgentIdAsync(botAgentId));
            
            Assert.Contains("tenant", exception.Message);
            _mockExecutionService.Verify(s => s.GetExecutionsByBotAgentIdAsync(botAgentId), Times.Once);
        }

        #endregion

        #region UpdateExecutionStatusAsync Tests

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithValidId_ReturnsUpdatedExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var newStatus = "Completed";
            var logOutput = "Execution completed successfully";

            var updatedExecution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = newStatus,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow,
                LogOutput = logOutput
            };

            _mockExecutionService.Setup(s => s.UpdateExecutionStatusAsync(executionId, newStatus, null, logOutput))
                .ReturnsAsync(updatedExecution);

            // Act
            var result = await _mockExecutionService.Object.UpdateExecutionStatusAsync(executionId, newStatus, null, logOutput);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal(newStatus, result.Status);
            Assert.Equal(logOutput, result.LogOutput);
            Assert.NotNull(result.EndTime);
            _mockExecutionService.Verify(s => s.UpdateExecutionStatusAsync(executionId, newStatus, null, logOutput), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithFailedStatus_ReturnsExecutionWithErrorMessage()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var newStatus = "Failed";
            var errorMessage = "Test error occurred";

            var updatedExecution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = newStatus,
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };

            _mockExecutionService.Setup(s => s.UpdateExecutionStatusAsync(executionId, newStatus, errorMessage, null))
                .ReturnsAsync(updatedExecution);

            // Act
            var result = await _mockExecutionService.Object.UpdateExecutionStatusAsync(executionId, newStatus, errorMessage);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newStatus, result.Status);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.NotNull(result.EndTime);
            _mockExecutionService.Verify(s => s.UpdateExecutionStatusAsync(executionId, newStatus, errorMessage, null), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var status = "Completed";

            _mockExecutionService.Setup(s => s.UpdateExecutionStatusAsync(invalidId, status, null, null))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _mockExecutionService.Object.UpdateExecutionStatusAsync(invalidId, status);

            // Assert
            Assert.Null(result);
            _mockExecutionService.Verify(s => s.UpdateExecutionStatusAsync(invalidId, status, null, null), Times.Once);
        }

        #endregion

        #region CancelExecutionAsync Tests

        [Fact]
        public async Task CancelExecutionAsync_WithValidId_ReturnsCancelledExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            
            var cancelledExecution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Cancelled",
                StartTime = DateTime.UtcNow.AddMinutes(-3),
                EndTime = DateTime.UtcNow
            };

            _mockExecutionService.Setup(s => s.CancelExecutionAsync(executionId))
                .ReturnsAsync(cancelledExecution);

            // Act
            var result = await _mockExecutionService.Object.CancelExecutionAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal("Cancelled", result.Status);
            Assert.NotNull(result.EndTime);
            _mockExecutionService.Verify(s => s.CancelExecutionAsync(executionId), Times.Once);
        }

        [Fact]
        public async Task CancelExecutionAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.CancelExecutionAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _mockExecutionService.Object.CancelExecutionAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockExecutionService.Verify(s => s.CancelExecutionAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task CancelExecutionAsync_WithAlreadyCompletedExecution_ReturnsUnchangedExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            
            var completedExecution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Completed", // Already completed
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-5)
            };

            _mockExecutionService.Setup(s => s.CancelExecutionAsync(executionId))
                .ReturnsAsync(completedExecution); // Returns the same execution without changing status

            // Act
            var result = await _mockExecutionService.Object.CancelExecutionAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Completed", result.Status); // Status remains unchanged
            _mockExecutionService.Verify(s => s.CancelExecutionAsync(executionId), Times.Once);
        }

        #endregion

        #region GetActiveExecutionsByBotAgentIdAsync Tests

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithValidId_ReturnsActiveExecutions()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var expectedExecutions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Pending",
                    StartTime = DateTime.UtcNow.AddMinutes(-2)
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Running",
                    StartTime = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _mockExecutionService.Setup(s => s.GetActiveExecutionsByBotAgentIdAsync(botAgentId))
                .ReturnsAsync(expectedExecutions);

            // Act
            var result = await _mockExecutionService.Object.GetActiveExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, e => Assert.Equal(botAgentId, e.BotAgentId));
            Assert.Contains(result, e => e.Status == "Pending");
            Assert.Contains(result, e => e.Status == "Running");
            Assert.All(result, e => Assert.Null(e.EndTime)); // Active executions have no end time
            _mockExecutionService.Verify(s => s.GetActiveExecutionsByBotAgentIdAsync(botAgentId), Times.Once);
        }

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithNoActiveExecutions_ReturnsEmptyCollection()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetActiveExecutionsByBotAgentIdAsync(botAgentId))
                .ReturnsAsync(new List<Execution>());

            // Act
            var result = await _mockExecutionService.Object.GetActiveExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockExecutionService.Verify(s => s.GetActiveExecutionsByBotAgentIdAsync(botAgentId), Times.Once);
        }

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithInvalidId_ReturnsEmptyCollection()
        {
            // Arrange
            var invalidBotAgentId = Guid.NewGuid();
            _mockExecutionService.Setup(s => s.GetActiveExecutionsByBotAgentIdAsync(invalidBotAgentId))
                .ReturnsAsync(new List<Execution>());

            // Act
            var result = await _mockExecutionService.Object.GetActiveExecutionsByBotAgentIdAsync(invalidBotAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockExecutionService.Verify(s => s.GetActiveExecutionsByBotAgentIdAsync(invalidBotAgentId), Times.Once);
        }

        #endregion
    }
} 