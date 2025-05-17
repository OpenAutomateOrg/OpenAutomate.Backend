using System;
using System.Threading.Tasks;
using Moq;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using Xunit;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class BotAgentServiceTests
    {
        private readonly Mock<IBotAgentService> _botAgentServiceMock;

        public BotAgentServiceTests()
        {
            _botAgentServiceMock = new Mock<IBotAgentService>();
        }

        [Fact]
        public async Task CreateBotAgentAsync_ShouldReturnExpectedResult()
        {
            // Arrange
            var expected = new BotAgentResponseDto { Name = "Agent1", MachineName = "M1", MachineKey = "key123" };
            _botAgentServiceMock.Setup(s => s.CreateBotAgentAsync(It.IsAny<CreateBotAgentDto>()))
                                .ReturnsAsync(expected);

            // Act
            var result = await _botAgentServiceMock.Object.CreateBotAgentAsync(new CreateBotAgentDto());

            // Assert
            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.MachineName, result.MachineName);
            Assert.Equal(expected.MachineKey, result.MachineKey);
        }

        [Fact]
        public async Task GetBotAgentByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _botAgentServiceMock.Setup(s => s.GetBotAgentByIdAsync(It.IsAny<Guid>()))
                                .ReturnsAsync((BotAgentResponseDto)null);

            // Act
            var result = await _botAgentServiceMock.Object.GetBotAgentByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllBotAgentsAsync_ShouldReturnList()
        {
            // Arrange
            var expected = new[]
            {
                new BotAgentResponseDto { Name = "Agent1", MachineName = "M1" },
                new BotAgentResponseDto { Name = "Agent2", MachineName = "M2" }
            };
            _botAgentServiceMock.Setup(s => s.GetAllBotAgentsAsync())
                .ReturnsAsync(expected);

            // Act
            var result = await _botAgentServiceMock.Object.GetAllBotAgentsAsync();

            // Assert
            Assert.Equal(2, System.Linq.Enumerable.Count(result));
        }

        [Fact]
        public async Task GetAllBotAgentsAsync_ShouldReturnEmpty_WhenNoAgents()
        {
            // Arrange
            _botAgentServiceMock.Setup(s => s.GetAllBotAgentsAsync())
                .ReturnsAsync(Array.Empty<BotAgentResponseDto>());

            // Act
            var result = await _botAgentServiceMock.Object.GetAllBotAgentsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task RegenerateMachineKeyAsync_ShouldReturnNewKey()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expected = new BotAgentResponseDto { Id = id, MachineKey = "newkey" };
            _botAgentServiceMock.Setup(s => s.RegenerateMachineKeyAsync(id))
                .ReturnsAsync(expected);

            // Act
            var result = await _botAgentServiceMock.Object.RegenerateMachineKeyAsync(id);

            // Assert
            Assert.Equal(id, result.Id);
            Assert.Equal("newkey", result.MachineKey);
        }

        [Fact]
        public async Task RegenerateMachineKeyAsync_ShouldThrow_WhenNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _botAgentServiceMock.Setup(s => s.RegenerateMachineKeyAsync(id))
                .ThrowsAsync(new ApplicationException("Not found"));

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _botAgentServiceMock.Object.RegenerateMachineKeyAsync(id));
        }



        [Fact]
        public async Task DeactivateBotAgentAsync_ShouldThrow_WhenServiceThrows()
        {
            // Arrange
            _botAgentServiceMock.Setup(s => s.DeactivateBotAgentAsync(It.IsAny<Guid>()))
                                .ThrowsAsync(new ApplicationException("Not found"));

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _botAgentServiceMock.Object.DeactivateBotAgentAsync(Guid.NewGuid()));
        }
    }
}
