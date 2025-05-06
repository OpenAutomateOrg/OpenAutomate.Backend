using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class BotAgentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITenantContext> _tenantContextMock;
        private readonly Mock<ILogger<BotAgentService>> _loggerMock;
        private readonly BotAgentService _service;

        public BotAgentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tenantContextMock = new Mock<ITenantContext>();
            _loggerMock = new Mock<ILogger<BotAgentService>>();
            _tenantContextMock.Setup(x => x.CurrentTenantId).Returns(Guid.NewGuid());
            _service = new BotAgentService(_unitOfWorkMock.Object, _tenantContextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateBotAgentAsync_ShouldCreateAndReturnBotAgent()
        {
            // Arrange
            var dto = new CreateBotAgentDto { Name = "Agent1", MachineName = "M1" };
            _unitOfWorkMock.Setup(u => u.BotAgents.AddAsync(It.IsAny<BotAgent>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreateBotAgentAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.MachineName, result.MachineName);
            Assert.False(string.IsNullOrEmpty(result.MachineKey));
        }

        [Fact]
        public async Task GetBotAgentByIdAsync_ShouldReturnNull_IfNotFound()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.BotAgents.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BotAgent)null);

            // Act
            var result = await _service.GetBotAgentByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllBotAgentsAsync_ShouldReturnBotAgentsOfCurrentTenant()
        {
            // Arrange
            var tenantId = _tenantContextMock.Object.CurrentTenantId;
            var botAgents = new List<BotAgent>
            {
                new BotAgent { Id = Guid.NewGuid(), Name = "A1", MachineName = "M1", MachineKey = "K1", Status = "Online", OrganizationUnitId = tenantId, IsActive = true },
                new BotAgent { Id = Guid.NewGuid(), Name = "A2", MachineName = "M2", MachineKey = "K2", Status = "Offline", OrganizationUnitId = tenantId, IsActive = true }
            };
            _unitOfWorkMock.Setup(u => u.BotAgents.GetAllAsync(
                           It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                           It.IsAny<System.Func<System.Linq.IQueryable<BotAgent>, System.Linq.IOrderedQueryable<BotAgent>>>(),
                           It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                           .ReturnsAsync(botAgents);

            // Act
            var result = await _service.GetAllBotAgentsAsync();

            // Assert
            Assert.Equal(2, System.Linq.Enumerable.Count(result));
        }

        [Fact]
        public async Task GetAllBotAgentsAsync_ShouldReturnEmpty_WhenNoBotAgents()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.BotAgents.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Func<System.Linq.IQueryable<BotAgent>, System.Linq.IOrderedQueryable<BotAgent>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync(new List<BotAgent>());

            // Act
            var result = await _service.GetAllBotAgentsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task RegenerateMachineKeyAsync_ShouldUpdateMachineKey_WhenFound()
        {
            // Arrange
            var tenantId = _tenantContextMock.Object.CurrentTenantId;
            var botAgent = new BotAgent { Id = Guid.NewGuid(), MachineKey = "OldKey", OrganizationUnitId = tenantId };
            _unitOfWorkMock.Setup(u => u.BotAgents.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(botAgent);
            _unitOfWorkMock.Setup(u => u.BotAgents.Update(It.IsAny<BotAgent>()));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.RegenerateMachineKeyAsync(botAgent.Id);

            // Assert
            Assert.NotEqual("OldKey", result.MachineKey);
            _unitOfWorkMock.Verify(u => u.BotAgents.Update(It.IsAny<BotAgent>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RegenerateMachineKeyAsync_ShouldThrow_IfNotFound()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.BotAgents.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BotAgent)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.RegenerateMachineKeyAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ValidateAndConnectBotAgentAsync_ShouldReturnBotAgent_WhenValid()
        {
            // Arrange
            var tenantSlug = "tenant1";
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = tenantSlug, IsActive = true };
            var request = new BotAgentConnectionRequest { MachineKey = "K1", MachineName = "M1" };
            var botAgent = new BotAgent { Id = Guid.NewGuid(), Name = "A1", MachineName = "M1", MachineKey = "K1", OrganizationUnitId = tenant.Id, IsActive = true };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(), 
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync(botAgent);
            _unitOfWorkMock.Setup(u => u.BotAgents.Update(It.IsAny<BotAgent>()));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ValidateAndConnectBotAgentAsync(request, tenantSlug);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(botAgent.Id, result.Id);
            Assert.Equal("Online", result.Status);
        }

        [Fact]
        public async Task ValidateAndConnectBotAgentAsync_ShouldThrow_WhenTenantNotFound()
        {
            // Arrange
            var request = new BotAgentConnectionRequest { MachineKey = "K1", MachineName = "M1" };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync((OrganizationUnit)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.ValidateAndConnectBotAgentAsync(request, "tenant1"));
        }

        [Fact]
        public async Task ValidateAndConnectBotAgentAsync_ShouldThrow_WhenBotAgentNotFound()
        {
            // Arrange
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = "tenant1", IsActive = true };
            var request = new BotAgentConnectionRequest { MachineKey = "K1", MachineName = "M1" };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync((BotAgent)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ValidateAndConnectBotAgentAsync(request, "tenant1"));
        }

        [Fact]
        public async Task UpdateBotAgentStatusAsync_ShouldUpdateStatus_WhenValid()
        {
            // Arrange
            var tenantSlug = "tenant1";
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = tenantSlug, IsActive = true };
            var request = new BotAgentStatusUpdateRequest { MachineKey = "K1", Status = "Busy", Timestamp = DateTime.UtcNow };
            var botAgent = new BotAgent { Id = Guid.NewGuid(), Name = "A1", MachineName = "M1", MachineKey = "K1", OrganizationUnitId = tenant.Id, IsActive = true };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync(botAgent);
            _unitOfWorkMock.Setup(u => u.BotAgents.Update(It.IsAny<BotAgent>()));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.UpdateBotAgentStatusAsync(request, tenantSlug);

            // Assert
            _unitOfWorkMock.Verify(u => u.BotAgents.Update(It.Is<BotAgent>(b => b.Status == request.Status)), Times.Once);
        }

        [Fact]
        public async Task UpdateBotAgentStatusAsync_ShouldThrow_WhenTenantNotFound()
        {
            // Arrange
            var request = new BotAgentStatusUpdateRequest { MachineKey = "K1", Status = "Busy", Timestamp = DateTime.UtcNow };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync((OrganizationUnit)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateBotAgentStatusAsync(request, "tenant1"));
        }

        [Fact]
        public async Task UpdateBotAgentStatusAsync_ShouldThrow_WhenBotAgentNotFound()
        {
            // Arrange
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = "tenant1", IsActive = true };
            var request = new BotAgentStatusUpdateRequest { MachineKey = "K1", Status = "Busy", Timestamp = DateTime.UtcNow };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync((BotAgent)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.UpdateBotAgentStatusAsync(request, "tenant1"));
        }

        [Fact]
        public async Task GetAssetsForBotAgentAsync_ShouldReturnAssets_WhenValid()
        {
            // Arrange
            var tenantSlug = "tenant1";
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = tenantSlug, IsActive = true };
            var botAgent = new BotAgent
            {
                Id = Guid.NewGuid(),
                Name = "A1",
                MachineName = "M1",
                MachineKey = "K1",
                OrganizationUnitId = tenant.Id,
                IsActive = true,
                AssetBotAgents = new List<AssetBotAgent> { new AssetBotAgent { AssetId = Guid.NewGuid() } }
            };
            var asset = new Asset { Id = botAgent.AssetBotAgents.First().AssetId, Name = "Asset1", Key = "Key1", Value = "Val1", Description = "Desc", IsEncrypted = false, OrganizationUnitId = tenant.Id };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync(botAgent);
            _unitOfWorkMock.Setup(u => u.Assets.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Asset, bool>>>(),
                It.IsAny<System.Func<System.Linq.IQueryable<Asset>, System.Linq.IOrderedQueryable<Asset>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Asset, object>>[]>()))
                .ReturnsAsync(new List<Asset> { asset });

            // Act
            var result = await _service.GetAssetsForBotAgentAsync("K1", tenantSlug);

            // Assert
            Assert.Single(result);
            Assert.Equal(asset.Id, System.Linq.Enumerable.First(result).Id);
        }

        [Fact]
        public async Task GetAssetsForBotAgentAsync_ShouldThrow_WhenTenantNotFound()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync((OrganizationUnit)null);

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.GetAssetsForBotAgentAsync("K1", "tenant1"));
        }

        [Fact]
        public async Task GetAssetsForBotAgentAsync_ShouldThrow_WhenBotAgentNotFound()
        {
            // Arrange
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = "tenant1", IsActive = true };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync((BotAgent)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetAssetsForBotAgentAsync("K1", "tenant1"));
        }

        [Fact]
        public async Task GetAssetsForBotAgentAsync_ShouldReturnEmpty_WhenNoAssets()
        {
            // Arrange
            var tenantSlug = "tenant1";
            var tenant = new OrganizationUnit { Id = Guid.NewGuid(), Slug = tenantSlug, IsActive = true };
            var botAgent = new BotAgent
            {
                Id = Guid.NewGuid(),
                Name = "A1",
                MachineName = "M1",
                MachineKey = "K1",
                OrganizationUnitId = tenant.Id,
                IsActive = true,
                AssetBotAgents = new List<AssetBotAgent>()
            };
            _unitOfWorkMock.Setup(u => u.OrganizationUnits.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<OrganizationUnit, bool>>>()))
                .ReturnsAsync(tenant);
            _unitOfWorkMock.Setup(u => u.BotAgents.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<BotAgent, object>>[]>()))
                .ReturnsAsync(botAgent);

            // Act
            var result = await _service.GetAssetsForBotAgentAsync("K1", tenantSlug);

            // Assert
            Assert.Empty(result);
        }
    }
}
