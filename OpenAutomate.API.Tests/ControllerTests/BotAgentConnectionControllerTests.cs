using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.API.Extensions;
using OpenAutomate.API.Hubs;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class BotAgentConnectionControllerTests
    {
        private readonly Mock<IBotAgentService> _mockBotAgentService;
        private readonly Mock<IHubContext<BotAgentHub>> _mockHubContext;
        private readonly Mock<ILogger<BotAgentConnectionController>> _mockLogger;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly BotAgentConnectionController _controller;

        public BotAgentConnectionControllerTests()
        {
            _mockBotAgentService = new Mock<IBotAgentService>();
            _mockHubContext = new Mock<IHubContext<BotAgentHub>>();
            _mockLogger = new Mock<ILogger<BotAgentConnectionController>>();
            _mockTenantContext = new Mock<ITenantContext>();
            _controller = new BotAgentConnectionController(
                _mockBotAgentService.Object,
                _mockHubContext.Object,
                _mockLogger.Object,
                _mockTenantContext.Object
            );
        }

        #region ConnectBotAgent

        [Fact]
        public async Task ConnectBotAgent_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ConnectBotAgent(null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Connection request is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ConnectBotAgent_MissingMachineKey_ReturnsBadRequest()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = null };

            // Act
            var result = await _controller.ConnectBotAgent(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Machine key is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ConnectBotAgent_InvalidTenantContext_ReturnsBadRequest()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = "key" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(false);

            // Act
            var result = await _controller.ConnectBotAgent(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid tenant context", badRequest.Value.ToString());
        }

        [Fact]
        public async Task ConnectBotAgent_InvalidMachineKey_ReturnsUnauthorized()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = "key" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockBotAgentService.Setup(s => s.GetAllBotAgentsAsync()).ReturnsAsync(new List<BotAgentResponseDto>());

            // Act
            var result = await _controller.ConnectBotAgent(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid machine key", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task ConnectBotAgent_Success_ReturnsOk()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = "key", MachineName = "Bot1" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            var botAgent = new BotAgentResponseDto { MachineKey = "key" };
            _mockBotAgentService.Setup(s => s.GetAllBotAgentsAsync()).ReturnsAsync(new List<BotAgentResponseDto> { botAgent });
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["tenant"] = "tenant1";
            _controller.ControllerContext = new ControllerContext { RouteData = routeData };

            // Act
            var result = await _controller.ConnectBotAgent(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("hubUrl", ok.Value.ToString());
        }

        [Fact]
        public async Task ConnectBotAgent_Exception_ReturnsServerError()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = "key" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockBotAgentService.Setup(s => s.GetAllBotAgentsAsync()).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.ConnectBotAgent(request);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error connecting bot agent", serverError.Value.ToString());
        }

        #endregion

        #region SendCommandToBotAgent

        [Fact]
        public async Task SendCommandToBotAgent_NullCommand_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Command data is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_MissingCommandType_ReturnsBadRequest()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = null };

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Command type is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_InvalidTenantContext_ReturnsBadRequest()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(false);

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid tenant context", badRequest.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_BotAgentNotFound_ReturnsNotFound()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BotAgentResponseDto)null);

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Bot agent not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_BotAgentOffline_ReturnsBadRequest()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            var botAgent = new BotAgentResponseDto { Status = "Offline" };
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(botAgent);

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("not online", badRequest.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_Success_ReturnsOk()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = "type", Payload = new { data = 1 } };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            var botAgent = new BotAgentResponseDto { Status = "Available" };
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(botAgent);

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error sending command", serverError.Value.ToString());
        }

        [Fact]
        public async Task SendCommandToBotAgent_Exception_ReturnsServerError()
        {
            // Arrange
            var command = new BotAgentCommandDto { CommandType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            var botAgent = new BotAgentResponseDto { Status = "Available" };
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(botAgent);

            // Act
            var result = await _controller.SendCommandToBotAgent(Guid.NewGuid(), command);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error sending command", serverError.Value.ToString());
        }

        #endregion

        #region GetBotAgentStatus

        [Fact]
        public async Task GetBotAgentStatus_InvalidTenantContext_ReturnsBadRequest()
        {
            // Arrange
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(false);

            // Act
            var result = await _controller.GetBotAgentStatus(Guid.NewGuid());

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid tenant context", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetBotAgentStatus_BotAgentNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BotAgentResponseDto)null);

            // Act
            var result = await _controller.GetBotAgentStatus(Guid.NewGuid());

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Bot agent not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetBotAgentStatus_Success_ReturnsOk()
        {
            // Arrange
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            var botAgent = new BotAgentResponseDto { Id = Guid.NewGuid(), Name = "Bot1", Status = "Available", LastConnected = DateTime.UtcNow };
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(botAgent);
            _mockTenantContext.SetupGet(t => t.CurrentTenantId).Returns(Guid.NewGuid());

            // Act
            var result = await _controller.GetBotAgentStatus(botAgent.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("status", ok.Value.ToString());
        }

        [Fact]
        public async Task GetBotAgentStatus_Exception_ReturnsServerError()
        {
            // Arrange
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>())).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.GetBotAgentStatus(Guid.NewGuid());

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error getting bot agent status", serverError.Value.ToString());
        }

        #endregion

        #region BroadcastNotification

        [Fact]
        public async Task BroadcastNotification_NullNotification_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.BroadcastNotification(null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Notification data is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task BroadcastNotification_InvalidTenantContext_ReturnsBadRequest()
        {
            // Arrange
            var notification = new BroadcastNotificationDto { NotificationType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(false);

            // Act
            var result = await _controller.BroadcastNotification(notification);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid tenant context", badRequest.Value.ToString());
        }

        [Fact]
        public async Task BroadcastNotification_MissingNotificationType_ReturnsBadRequest()
        {
            // Arrange
            var notification = new BroadcastNotificationDto { NotificationType = null };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);

            // Act
            var result = await _controller.BroadcastNotification(notification);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Notification type is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task BroadcastNotification_Success_ReturnsOk()
        {
            // Arrange
            var notification = new BroadcastNotificationDto { NotificationType = "type", Data = new { data = 1 } };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);
            _mockTenantContext.SetupGet(t => t.CurrentTenantId).Returns(Guid.NewGuid());

            // Act
            var result = await _controller.BroadcastNotification(notification);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error broadcasting notification", serverError.Value.ToString());
        }

        [Fact]
        public async Task BroadcastNotification_Exception_ReturnsServerError()
        {
            // Arrange
            var notification = new BroadcastNotificationDto { NotificationType = "type" };
            _mockTenantContext.SetupGet(t => t.HasTenant).Returns(true);

            // Act
            var result = await _controller.BroadcastNotification(notification);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("Error broadcasting notification", serverError.Value.ToString());
        }

        #endregion
    }
}