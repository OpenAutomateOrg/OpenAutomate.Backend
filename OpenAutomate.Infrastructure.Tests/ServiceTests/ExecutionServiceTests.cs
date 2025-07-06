using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class ExecutionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<Execution>> _mockExecutionRepository;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<ExecutionService>> _mockLogger;
        private readonly ExecutionService _executionService;
        private readonly Guid _tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public ExecutionServiceTests()
        {
            _mockExecutionRepository = new Mock<IRepository<Execution>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUnitOfWork.Setup(u => u.Executions).Returns(_mockExecutionRepository.Object);
            
            _mockTenantContext = new Mock<ITenantContext>();
            _mockTenantContext.Setup(t => t.HasTenant).Returns(true);
            _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(_tenantId);
            
            _mockLogger = new Mock<ILogger<ExecutionService>>();
            
            _executionService = new ExecutionService(
                _mockUnitOfWork.Object,
                _mockTenantContext.Object,
                _mockLogger.Object);
        }

        #region CreateExecutionAsync Tests

        [Fact]
        public async Task CreateExecutionAsync_WithValidData_CreatesExecution()
        {
            // Arrange
            var createDto = new CreateExecutionDto
            {
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid()
            };

            Execution savedExecution = null;
            _mockExecutionRepository.Setup(r => r.AddAsync(It.IsAny<Execution>()))
                .Callback<Execution>(e => savedExecution = e)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _executionService.CreateExecutionAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.BotAgentId, result.BotAgentId);
            Assert.Equal(createDto.PackageId, result.PackageId);
            Assert.Equal("Pending", result.Status);
            Assert.Equal(_tenantId, result.OrganizationUnitId);
            Assert.NotEqual(default, result.StartTime);
            Assert.Null(result.EndTime);
            
            _mockExecutionRepository.Verify(r => r.AddAsync(It.IsAny<Execution>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExecutionAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            _mockTenantContext.Setup(t => t.HasTenant).Returns(false);
            
            var createDto = new CreateExecutionDto
            {
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _executionService.CreateExecutionAsync(createDto));
            
            Assert.Contains("tenant", exception.Message);
            _mockExecutionRepository.Verify(r => r.AddAsync(It.IsAny<Execution>()), Times.Never);
        }

        #endregion

        #region GetExecutionByIdAsync Tests

        [Fact]
        public async Task GetExecutionByIdAsync_WithValidId_ReturnsExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow,
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.GetExecutionByIdAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(executionId, result.Id);
            Assert.Equal(_tenantId, result.OrganizationUnitId);
            _mockExecutionRepository.Verify(r => r.GetByIdAsync(executionId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockExecutionRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _executionService.GetExecutionByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockExecutionRepository.Verify(r => r.GetByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionByIdAsync_WithDifferentTenant_ReturnsNull()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var differentTenantId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow,
                OrganizationUnitId = differentTenantId // Different tenant
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.GetExecutionByIdAsync(executionId);

            // Assert
            Assert.Null(result);
            _mockExecutionRepository.Verify(r => r.GetByIdAsync(executionId), Times.Once);
        }

        [Fact]
        public async Task GetExecutionByIdAsync_WithNoTenant_ReturnsNull()
        {
            // Arrange
            _mockTenantContext.Setup(t => t.HasTenant).Returns(false);
            
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow,
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.GetExecutionByIdAsync(executionId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllExecutionsAsync Tests

        [Fact]
        public async Task GetAllExecutionsAsync_ReturnsAllExecutionsForTenant()
        {
            // Arrange
            var executions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = Guid.NewGuid(),
                    PackageId = Guid.NewGuid(),
                    Status = "Completed",
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    EndTime = DateTime.UtcNow.AddMinutes(-30),
                    OrganizationUnitId = _tenantId
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = Guid.NewGuid(),
                    PackageId = Guid.NewGuid(),
                    Status = "Running",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    OrganizationUnitId = _tenantId
                }
            };

            _mockExecutionRepository.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Execution, bool>>>(),
                    null,
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _executionService.GetAllExecutionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, e => e.Status == "Completed");
            Assert.Contains(result, e => e.Status == "Running");
            _mockExecutionRepository.Verify(r => r.GetAllAsync(
                It.Is<Expression<Func<Execution, bool>>>(expr => expr.ToString().Contains("OrganizationUnitId")),
                null,
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetAllExecutionsAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            _mockTenantContext.Setup(t => t.HasTenant).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _executionService.GetAllExecutionsAsync());
            
            Assert.Contains("tenant", exception.Message);
        }

        [Fact]
        public async Task GetAllExecutionsAsync_WithNoExecutions_ReturnsEmptyCollection()
        {
            // Arrange
            _mockExecutionRepository.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Execution, bool>>>(),
                    null,
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>()))
                .ReturnsAsync(new List<Execution>());

            // Act
            var result = await _executionService.GetAllExecutionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetExecutionsByBotAgentIdAsync Tests

        [Fact]
        public async Task GetExecutionsByBotAgentIdAsync_WithValidId_ReturnsExecutions()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var executions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Completed",
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddHours(1),
                    OrganizationUnitId = _tenantId
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Failed",
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    EndTime = DateTime.UtcNow.AddHours(-1),
                    ErrorMessage = "Test error",
                    OrganizationUnitId = _tenantId
                }
            };

            _mockExecutionRepository.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Execution, bool>>>(),
                    null,
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _executionService.GetExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, e => Assert.Equal(botAgentId, e.BotAgentId));
            _mockExecutionRepository.Verify(r => r.GetAllAsync(
                It.Is<Expression<Func<Execution, bool>>>(expr => 
                    expr.ToString().Contains("BotAgentId") && 
                    expr.ToString().Contains("OrganizationUnitId")),
                null,
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetExecutionsByBotAgentIdAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            _mockTenantContext.Setup(t => t.HasTenant).Returns(false);
            var botAgentId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _executionService.GetExecutionsByBotAgentIdAsync(botAgentId));
            
            Assert.Contains("tenant", exception.Message);
        }

        #endregion

        #region UpdateExecutionStatusAsync Tests

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithCompletedStatus_UpdatesStatusAndEndTime()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.UpdateExecutionStatusAsync(executionId, "Completed");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Completed", result.Status);
            Assert.NotNull(result.EndTime);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithFailedStatus_UpdatesStatusAndErrorMessage()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var errorMessage = "Test error occurred";
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.UpdateExecutionStatusAsync(executionId, "Failed", errorMessage);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.Status);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.NotNull(result.EndTime);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithRunningStatus_UpdatesStatusOnly()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var logOutput = "Process started...";
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Pending",
                StartTime = DateTime.UtcNow.AddMinutes(-1),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.UpdateExecutionStatusAsync(executionId, "Running", null, logOutput);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Running", result.Status);
            Assert.Equal(logOutput, result.LogOutput);
            Assert.Null(result.EndTime);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionStatusAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockExecutionRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _executionService.UpdateExecutionStatusAsync(invalidId, "Completed");

            // Assert
            Assert.Null(result);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region UpdateExecutionLogPathAsync Tests

        [Fact]
        public async Task UpdateExecutionLogPathAsync_WithValidId_UpdatesLogPath()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var logS3Path = "s3://logs/execution-123.log";
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Completed",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow,
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.UpdateExecutionLogPathAsync(executionId, logS3Path);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(logS3Path, result.LogS3Path);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateExecutionLogPathAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var logS3Path = "s3://logs/execution-123.log";
            
            _mockExecutionRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _executionService.UpdateExecutionLogPathAsync(invalidId, logS3Path);

            // Assert
            Assert.Null(result);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region CancelExecutionAsync Tests

        [Fact]
        public async Task CancelExecutionAsync_WithPendingExecution_CancelsExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Pending",
                StartTime = DateTime.UtcNow.AddMinutes(-3),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.CancelExecutionAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cancelled", result.Status);
            Assert.NotNull(result.EndTime);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelExecutionAsync_WithRunningExecution_CancelsExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Running",
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.CancelExecutionAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cancelled", result.Status);
            Assert.NotNull(result.EndTime);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelExecutionAsync_WithCompletedExecution_DoesNotCancelExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var execution = new Execution
            {
                Id = executionId,
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Status = "Completed",
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-5),
                OrganizationUnitId = _tenantId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId))
                .ReturnsAsync(execution);

            // Act
            var result = await _executionService.CancelExecutionAsync(executionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Completed", result.Status); // Status remains unchanged
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelExecutionAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockExecutionRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Execution)null);

            // Act
            var result = await _executionService.CancelExecutionAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region GetActiveExecutionsByBotAgentIdAsync Tests

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithValidId_ReturnsActiveExecutions()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var executions = new List<Execution>
            {
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Pending",
                    StartTime = DateTime.UtcNow.AddMinutes(-2),
                    OrganizationUnitId = _tenantId
                },
                new Execution
                {
                    Id = Guid.NewGuid(),
                    BotAgentId = botAgentId,
                    PackageId = Guid.NewGuid(),
                    Status = "Running",
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    OrganizationUnitId = _tenantId
                }
            };

            _mockExecutionRepository.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Execution, bool>>>(),
                    null,
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _executionService.GetActiveExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, e => Assert.Equal(botAgentId, e.BotAgentId));
            Assert.Contains(result, e => e.Status == "Pending");
            Assert.Contains(result, e => e.Status == "Running");
            _mockExecutionRepository.Verify(r => r.GetAllAsync(
                It.Is<Expression<Func<Execution, bool>>>(expr => 
                    expr.ToString().Contains("BotAgentId") && 
                    expr.ToString().Contains("OrganizationUnitId") &&
                    expr.ToString().Contains("Status")),
                null,
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>(),
                It.IsAny<Expression<Func<Execution, object>>>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithNoTenant_ThrowsException()
        {
            // Arrange
            _mockTenantContext.Setup(t => t.HasTenant).Returns(false);
            var botAgentId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _executionService.GetActiveExecutionsByBotAgentIdAsync(botAgentId));
            
            Assert.Contains("tenant", exception.Message);
        }

        [Fact]
        public async Task GetActiveExecutionsByBotAgentIdAsync_WithNoActiveExecutions_ReturnsEmptyCollection()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            _mockExecutionRepository.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Execution, bool>>>(),
                    null,
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>(),
                    It.IsAny<Expression<Func<Execution, object>>>()))
                .ReturnsAsync(new List<Execution>());

            // Act
            var result = await _executionService.GetActiveExecutionsByBotAgentIdAsync(botAgentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
} 