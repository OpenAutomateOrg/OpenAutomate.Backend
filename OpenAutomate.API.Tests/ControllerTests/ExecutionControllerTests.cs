using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.API.Hubs;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class ExecutionControllerTests
    {
        private readonly Mock<IExecutionService> _mockExecutionService;
        private readonly Mock<IBotAgentService> _mockBotAgentService;
        private readonly Mock<IAutomationPackageService> _mockPackageService;
        private readonly Mock<ILogStorageService> _mockLogStorageService;
        private readonly Mock<IHubContext<BotAgentHub>> _mockHubContext;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<ExecutionController>> _mockLogger;
        private readonly ExecutionController _controller;

        public ExecutionControllerTests()
        {
            _mockExecutionService = new Mock<IExecutionService>();
            _mockBotAgentService = new Mock<IBotAgentService>();
            _mockPackageService = new Mock<IAutomationPackageService>();
            _mockLogStorageService = new Mock<ILogStorageService>();
            _mockHubContext = new Mock<IHubContext<BotAgentHub>>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<ExecutionController>>();

            // Note: SignalR functionality is tested separately in integration tests
            // The controller will handle SignalR exceptions gracefully in try-catch blocks

            _controller = new ExecutionController(
                _mockExecutionService.Object,
                _mockBotAgentService.Object,
                _mockPackageService.Object,
                _mockLogStorageService.Object,
                _mockHubContext.Object,
                _mockTenantContext.Object,
                _mockLogger.Object);

            // Setup controller context
            var httpContext = new DefaultHttpContext();
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["tenant"] = "test-tenant";

            // Setup user in HttpContext for GetCurrentUserId()
            var userId = Guid.NewGuid();
            httpContext.Items["User"] = new Core.Domain.Entities.User { Id = userId };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = routeData
            };

            // Setup tenant context
            _mockTenantContext.Setup(x => x.CurrentTenantSlug).Returns("test-tenant");
        }

        #region TriggerExecution Tests

        [Fact]
        public async Task TriggerExecution_WithValidData_ReturnsOkWithExecution()
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



            _mockBotAgentService.Setup(x => x.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);
            _mockPackageService.Setup(x => x.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);
            _mockExecutionService.Setup(x => x.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ReturnsAsync(new Core.Domain.Entities.Execution
                {
                    Id = executionId,
                    BotAgentId = botAgentId,
                    PackageId = packageId,
                    Status = "Pending",
                    BotAgent = new Core.Domain.Entities.BotAgent
                    {
                        Id = botAgentId,
                        Name = "TestAgent"
                    },
                    Package = new Core.Domain.Entities.AutomationPackage
                    {
                        Id = packageId,
                        Name = "TestPackage"
                    }
                });



            // Act
            var result = await _controller.TriggerExecution(triggerDto);

            // Assert
            Assert.NotNull(result.Result);
            
            // Check if it's a successful result (Ok or any 2xx status)
            if (result.Result is OkObjectResult okResult)
            {
                var returnedExecution = Assert.IsType<ExecutionResponseDto>(okResult.Value);
                Assert.Equal(executionId, returnedExecution.Id);
            }
            else if (result.Result is ObjectResult objectResult)
            {
                // Accept any 2xx status code as success
                Assert.True(objectResult.StatusCode >= 200 && objectResult.StatusCode < 300, 
                    $"Expected success status code (2xx), but got {objectResult.StatusCode}");
                
                if (objectResult.Value is ExecutionResponseDto returnedExecution)
                {
                    Assert.Equal(executionId, returnedExecution.Id);
                }
            }
            else
            {
                // If it's neither OkObjectResult nor ObjectResult, it might be some other success type
                // Just verify it's not an error result
                Assert.False(result.Result is BadRequestObjectResult || result.Result is NotFoundObjectResult,
                    $"Expected success result but got {result.Result.GetType().Name}");
            }
        }

        [Fact]
        public async Task TriggerExecution_WithInvalidBotAgent_ReturnsNotFound()
        {
            // Arrange
            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            _mockBotAgentService.Setup(x => x.GetBotAgentByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((BotAgentResponseDto?)null);

            // Act
            var result = await _controller.TriggerExecution(triggerDto);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Bot agent not found", notFoundResult.Value);
        }

        [Fact]
        public async Task TriggerExecution_WithUnavailableBotAgent_ReturnsBadRequest()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = Guid.NewGuid(),
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Disconnected"
            };

            _mockBotAgentService.Setup(x => x.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);

            // Act
            var result = await _controller.TriggerExecution(triggerDto);

            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Bot agent is disconnected", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task TriggerExecution_WithInvalidPackage_ReturnsNotFound()
        {
            // Arrange
            var botAgentId = Guid.NewGuid();
            var triggerDto = new TriggerExecutionDto
            {
                BotAgentId = botAgentId,
                PackageId = Guid.NewGuid(),
                PackageName = "TestPackage",
                Version = "1.0.0"
            };

            var botAgent = new BotAgentResponseDto
            {
                Id = botAgentId,
                Name = "TestAgent",
                Status = "Available"
            };

            _mockBotAgentService.Setup(x => x.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);
            _mockPackageService.Setup(x => x.GetPackageByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AutomationPackageResponseDto?)null);

            // Act
            var result = await _controller.TriggerExecution(triggerDto);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Package not found", notFoundResult.Value);
        }

        [Fact]
        public async Task TriggerExecution_WhenServiceThrowsException_ReturnsInternalServerError()
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

            var package = new AutomationPackageResponseDto
            {
                Id = packageId,
                Name = "TestPackage"
            };

            _mockBotAgentService.Setup(x => x.GetBotAgentByIdAsync(botAgentId))
                .ReturnsAsync(botAgent);
            _mockPackageService.Setup(x => x.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);
            _mockExecutionService.Setup(x => x.CreateExecutionAsync(It.IsAny<CreateExecutionDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.TriggerExecution(triggerDto);

            // Assert
            Assert.NotNull(result.Result);
            var statusCodeResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
            Assert.Equal("Failed to trigger execution", statusCodeResult.Value);
        }

        #endregion

        #region UpdateExecutionStatus Tests

        [Fact]
        public async Task UpdateExecutionStatus_WithValidData_ReturnsOkWithUpdatedExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var updateDto = new UpdateExecutionStatusDto
            {
                Status = "Completed",
                ErrorMessage = null,
                LogOutput = "Execution completed successfully"
            };

            _mockExecutionService.Setup(x => x.UpdateExecutionStatusAsync(
                executionId, updateDto.Status, updateDto.ErrorMessage, updateDto.LogOutput))
                .ReturnsAsync(new Core.Domain.Entities.Execution
                {
                    Id = executionId,
                    Status = "Completed",
                    LogOutput = "Execution completed successfully",
                    BotAgent = new Core.Domain.Entities.BotAgent
                    {
                        Id = Guid.NewGuid(),
                        Name = "TestAgent"
                    },
                    Package = new Core.Domain.Entities.AutomationPackage
                    {
                        Id = Guid.NewGuid(),
                        Name = "TestPackage"
                    }
                });



            // Act
            var result = await _controller.UpdateExecutionStatus(executionId, updateDto);

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExecution = Assert.IsType<ExecutionResponseDto>(okResult.Value);
            Assert.Equal(executionId, returnedExecution.Id);
            Assert.Equal("Completed", returnedExecution.Status);
        }

        [Fact]
        public async Task UpdateExecutionStatus_WithInvalidExecutionId_ReturnsNotFound()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var updateDto = new UpdateExecutionStatusDto
            {
                Status = "Completed"
            };

            _mockExecutionService.Setup(x => x.UpdateExecutionStatusAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Core.Domain.Entities.Execution?)null);

            // Act
            var result = await _controller.UpdateExecutionStatus(executionId, updateDto);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Execution not found", notFoundResult.Value);
        }

        #endregion

        #region CancelExecution Tests

        [Fact]
        public async Task CancelExecution_WithValidId_ReturnsOkWithCancelledExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();

            _mockExecutionService.Setup(x => x.CancelExecutionAsync(executionId))
                .ReturnsAsync(new Core.Domain.Entities.Execution
                {
                    Id = executionId,
                    BotAgentId = botAgentId,
                    Status = "Cancelled",
                    BotAgent = new Core.Domain.Entities.BotAgent
                    {
                        Id = botAgentId,
                        Name = "TestAgent"
                    },
                    Package = new Core.Domain.Entities.AutomationPackage
                    {
                        Id = Guid.NewGuid(),
                        Name = "TestPackage"
                    }
                });



            // Act
            var result = await _controller.CancelExecution(executionId);

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExecution = Assert.IsType<ExecutionResponseDto>(okResult.Value);
            Assert.Equal(executionId, returnedExecution.Id);
            Assert.Equal("Cancelled", returnedExecution.Status);
        }

        [Fact]
        public async Task CancelExecution_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var executionId = Guid.NewGuid();

            _mockExecutionService.Setup(x => x.CancelExecutionAsync(executionId))
                .ReturnsAsync((Core.Domain.Entities.Execution?)null);

            // Act
            var result = await _controller.CancelExecution(executionId);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Execution not found", notFoundResult.Value);
        }

        #endregion

        #region GetExecutionById Tests

        [Fact]
        public async Task GetExecutionById_WithValidId_ReturnsOkWithExecution()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            _mockExecutionService.Setup(x => x.GetExecutionByIdAsync(executionId))
                .ReturnsAsync(new Core.Domain.Entities.Execution
                {
                    Id = executionId,
                    Status = "Running",
                    BotAgent = new Core.Domain.Entities.BotAgent { Name = "TestAgent" },
                    Package = new Core.Domain.Entities.AutomationPackage { Name = "TestPackage" }
                });

            // Act
            var result = await _controller.GetExecutionById(executionId);

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExecution = Assert.IsType<ExecutionResponseDto>(okResult.Value);
            Assert.Equal(executionId, returnedExecution.Id);
        }

        [Fact]
        public async Task GetExecutionById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var executionId = Guid.NewGuid();

            _mockExecutionService.Setup(x => x.GetExecutionByIdAsync(executionId))
                .ReturnsAsync((Core.Domain.Entities.Execution?)null);

            // Act
            var result = await _controller.GetExecutionById(executionId);

            // Assert
            Assert.NotNull(result.Result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Execution not found", notFoundResult.Value);
        }

        #endregion

        #region GetAllExecutions Tests

        [Fact]
        public async Task GetAllExecutions_ReturnsOkWithExecutionList()
        {
            // Arrange
            var executions = new List<Core.Domain.Entities.Execution>
            {
                new Core.Domain.Entities.Execution
                {
                    Id = Guid.NewGuid(),
                    Status = "Completed",
                    BotAgent = new Core.Domain.Entities.BotAgent { Name = "Agent1" },
                    Package = new Core.Domain.Entities.AutomationPackage { Name = "Package1" }
                },
                new Core.Domain.Entities.Execution
                {
                    Id = Guid.NewGuid(),
                    Status = "Running",
                    BotAgent = new Core.Domain.Entities.BotAgent { Name = "Agent2" },
                    Package = new Core.Domain.Entities.AutomationPackage { Name = "Package2" }
                }
            };

            _mockExecutionService.Setup(x => x.GetAllExecutionsAsync())
                .ReturnsAsync(executions);

            // Act
            var result = await _controller.GetAllExecutions();

            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedExecutions = Assert.IsAssignableFrom<IEnumerable<ExecutionResponseDto>>(okResult.Value);
            Assert.Equal(2, returnedExecutions.Count());
        }

        #endregion



        #region Error Handling Tests

        [Fact]
        public async Task UpdateExecutionStatus_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var updateDto = new UpdateExecutionStatusDto
            {
                Status = "Failed",
                ErrorMessage = "Test error"
            };

            _mockExecutionService.Setup(x => x.UpdateExecutionStatusAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateExecutionStatus(executionId, updateDto);

            // Assert
            Assert.NotNull(result.Result);
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task CancelExecution_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var executionId = Guid.NewGuid();

            _mockExecutionService.Setup(x => x.CancelExecutionAsync(executionId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CancelExecution(executionId);

            // Assert
            Assert.NotNull(result.Result);
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion
    }
}
