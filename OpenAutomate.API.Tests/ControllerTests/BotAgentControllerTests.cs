using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class BotAgentControllerTests
    {
        private readonly Mock<IBotAgentService> _mockBotAgentService;
        private readonly BotAgentController _controller;

        public BotAgentControllerTests()
        {
            _mockBotAgentService = new Mock<IBotAgentService>();
            _controller = new BotAgentController(_mockBotAgentService.Object);
        }

        [Fact]
        public async Task CreateBotAgent_WithValidRequest_ReturnsCreatedWithAgent()
        {
            // Arrange
            var createDto = new CreateBotAgentDto
            {
                Name = "Test Agent",
                MachineName = "TEST-MACHINE-01"
            };
            var expectedResponse = new BotAgentResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Agent",
                MachineName = "TEST-MACHINE-01",
                MachineKey = "test-machine-key",
                IsActive = true
            };

            _mockBotAgentService.Setup(s => s.CreateBotAgentAsync(createDto))
                .ReturnsAsync(expectedResponse);

            // Mock tenant in route data
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData()
            };
            _controller.RouteData.Values["tenant"] = "test-tenant";

            // Act
            var result = await _controller.CreateBotAgent(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<BotAgentResponseDto>(createdResult.Value);
            Assert.Equal(expectedResponse.Id, returnValue.Id);
            Assert.Equal(expectedResponse.Name, returnValue.Name);
            Assert.Equal(expectedResponse.MachineName, returnValue.MachineName);
            Assert.Equal(expectedResponse.MachineKey, returnValue.MachineKey);
            Assert.Equal(expectedResponse.IsActive, returnValue.IsActive);
            _mockBotAgentService.Verify(s => s.CreateBotAgentAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateBotAgent_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var createDto = new CreateBotAgentDto
            {
                Name = "Test Agent",
                MachineName = "TEST-MACHINE-01"
            };

            _mockBotAgentService.Setup(s => s.CreateBotAgentAsync(createDto))
                .ThrowsAsync(new Exception("Unexpected error"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData()
            };
            _controller.RouteData.Values["tenant"] = "test-tenant";

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.CreateBotAgent(createDto));
            _mockBotAgentService.Verify(s => s.CreateBotAgentAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task GetBotAgentById_WithValidId_ReturnsAgent()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var expectedAgent = new BotAgentResponseDto
            {
                Id = agentId,
                Name = "Test Agent",
                MachineName = "TEST-MACHINE-01",
                MachineKey = "test-machine-key",
                IsActive = true
            };

            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(agentId))
                .ReturnsAsync(expectedAgent);

            // Act
            var result = await _controller.GetBotAgentById(agentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<BotAgentResponseDto>(okResult.Value);
            Assert.Equal(expectedAgent.Id, returnValue.Id);
            Assert.Equal(expectedAgent.Name, returnValue.Name);
            Assert.Equal(expectedAgent.MachineName, returnValue.MachineName);
            Assert.Equal(expectedAgent.MachineKey, returnValue.MachineKey);
            Assert.Equal(expectedAgent.IsActive, returnValue.IsActive);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(agentId), Times.Once);
        }

        [Fact]
        public async Task GetBotAgentById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(nonExistentId))
                .ReturnsAsync((BotAgentResponseDto)null);

            // Act
            var result = await _controller.GetBotAgentById(nonExistentId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task GetBotAgentById_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var expectedErrorMessage = "Unexpected error in GetBotAgentById";
            _mockBotAgentService.Setup(s => s.GetBotAgentByIdAsync(agentId))
                .ThrowsAsync(new Exception(expectedErrorMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.GetBotAgentById(agentId));
            Assert.Equal(expectedErrorMessage, exception.Message);
            _mockBotAgentService.Verify(s => s.GetBotAgentByIdAsync(agentId), Times.Once);
        }
    }
}
