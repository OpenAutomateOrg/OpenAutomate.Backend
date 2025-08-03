using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.IServices;
using Xunit;

namespace OpenAutomate.Core.Tests.IserviceTest
{
    public class IAssetServiceTests
    {
        private readonly Mock<IAssetService> _mockAssetService;

        public IAssetServiceTests()
        {
            _mockAssetService = new Mock<IAssetService>();
        }

        #region CreateAssetAsync Tests

        [Fact]
        public async Task CreateAssetAsync_WithValidData_ReturnsCreatedAsset()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "test-key",
                Value = "test-value",
                Description = "Test asset description",
                Type = AssetType.String,
                BotAgentIds = new List<Guid> { Guid.NewGuid() }
            };

            var expectedAsset = new AssetResponseDto
            {
                Id = Guid.NewGuid(),
                Key = "test-key",
                Value = "test-value",
                Description = "Test asset description",
                IsEncrypted = false,
                Type = AssetType.String,
                CreatedAt = DateTime.UtcNow
            };

            _mockAssetService.Setup(s => s.CreateAssetAsync(createDto))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _mockAssetService.Object.CreateAssetAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedAsset.Id, result.Id);
            Assert.Equal("test-key", result.Key);
            Assert.Equal("test-value", result.Value);
            Assert.Equal("Test asset description", result.Description);
            Assert.False(result.IsEncrypted);
            Assert.Equal(AssetType.String, result.Type);
            _mockAssetService.Verify(s => s.CreateAssetAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateAssetAsync_WithSecretType_ReturnsEncryptedAsset()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "secret-key",
                Value = "secret-value",
                Description = "Secret asset",
                Type = AssetType.Secret
            };

            var expectedAsset = new AssetResponseDto
            {
                Id = Guid.NewGuid(),
                Key = "secret-key",
                Value = "encrypted-value", // In a real scenario, this would be encrypted
                Description = "Secret asset",
                IsEncrypted = true,
                Type = AssetType.Secret,
                CreatedAt = DateTime.UtcNow
            };

            _mockAssetService.Setup(s => s.CreateAssetAsync(createDto))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _mockAssetService.Object.CreateAssetAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("secret-key", result.Key);
            Assert.True(result.IsEncrypted);
            Assert.Equal(AssetType.Secret, result.Type);
            _mockAssetService.Verify(s => s.CreateAssetAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateAssetAsync_WithDuplicateKey_ThrowsException()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "existing-key",
                Value = "test-value",
                Description = "Duplicate key asset"
            };

            _mockAssetService.Setup(s => s.CreateAssetAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("Asset with key 'existing-key' already exists"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mockAssetService.Object.CreateAssetAsync(createDto));
            
            Assert.Contains("already exists", exception.Message);
            _mockAssetService.Verify(s => s.CreateAssetAsync(createDto), Times.Once);
        }

        #endregion

        #region GetAllAssetsAsync Tests

        [Fact]
        public async Task GetAllAssetsAsync_ReturnsAllAssets()
        {
            // Arrange
            var expectedAssets = new List<AssetListResponseDto>
            {
                new AssetListResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Key = "asset1", 
                    Description = "First asset", 
                    Type = AssetType.String,
                    IsEncrypted = false,
                    CreatedAt = DateTime.UtcNow,
                    AuthorizedBotAgentsCount = 2
                },
                new AssetListResponseDto 
                { 
                    Id = Guid.NewGuid(), 
                    Key = "asset2", 
                    Description = "Second asset", 
                    Type = AssetType.Secret,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow,
                    AuthorizedBotAgentsCount = 1
                }
            };

            _mockAssetService.Setup(s => s.GetAllAssetsAsync())
                .ReturnsAsync(expectedAssets);

            // Act
            var result = await _mockAssetService.Object.GetAllAssetsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Key == "asset1");
            Assert.Contains(result, a => a.Key == "asset2");
            _mockAssetService.Verify(s => s.GetAllAssetsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAssetsAsync_ReturnsEmptyCollection_WhenNoAssetsExist()
        {
            // Arrange
            var emptyList = new List<AssetListResponseDto>();
            _mockAssetService.Setup(s => s.GetAllAssetsAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _mockAssetService.Object.GetAllAssetsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockAssetService.Verify(s => s.GetAllAssetsAsync(), Times.Once);
        }

        #endregion

        #region GetAssetByIdAsync Tests

        [Fact]
        public async Task GetAssetByIdAsync_WithValidId_ReturnsAsset()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var expectedAsset = new AssetResponseDto
            {
                Id = assetId,
                Key = "test-key",
                Value = "test-value",
                Description = "Test asset",
                IsEncrypted = false,
                Type = AssetType.String,
                CreatedAt = DateTime.UtcNow
            };

            _mockAssetService.Setup(s => s.GetAssetByIdAsync(assetId))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _mockAssetService.Object.GetAssetByIdAsync(assetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(assetId, result.Id);
            Assert.Equal("test-key", result.Key);
            _mockAssetService.Verify(s => s.GetAssetByIdAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task GetAssetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockAssetService.Setup(s => s.GetAssetByIdAsync(invalidId))
                .ReturnsAsync((AssetResponseDto)null);

            // Act
            var result = await _mockAssetService.Object.GetAssetByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockAssetService.Verify(s => s.GetAssetByIdAsync(invalidId), Times.Once);
        }

        #endregion

        #region GetAssetByKeyAsync Tests

        [Fact]
        public async Task GetAssetByKeyAsync_WithValidKey_ReturnsAsset()
        {
            // Arrange
            var assetKey = "test-key";
            var expectedAsset = new AssetResponseDto
            {
                Id = Guid.NewGuid(),
                Key = assetKey,
                Value = "test-value",
                Description = "Test asset by key",
                IsEncrypted = false,
                Type = AssetType.String,
                CreatedAt = DateTime.UtcNow
            };

            _mockAssetService.Setup(s => s.GetAssetByKeyAsync(assetKey))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _mockAssetService.Object.GetAssetByKeyAsync(assetKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(assetKey, result.Key);
            Assert.Equal("test-value", result.Value);
            _mockAssetService.Verify(s => s.GetAssetByKeyAsync(assetKey), Times.Once);
        }

        [Fact]
        public async Task GetAssetByKeyAsync_WithInvalidKey_ReturnsNull()
        {
            // Arrange
            var invalidKey = "invalid-key";
            _mockAssetService.Setup(s => s.GetAssetByKeyAsync(invalidKey))
                .ReturnsAsync((AssetResponseDto)null);

            // Act
            var result = await _mockAssetService.Object.GetAssetByKeyAsync(invalidKey);

            // Assert
            Assert.Null(result);
            _mockAssetService.Verify(s => s.GetAssetByKeyAsync(invalidKey), Times.Once);
        }

        #endregion

        #region UpdateAssetAsync Tests

        [Fact]
        public async Task UpdateAssetAsync_WithValidData_ReturnsUpdatedAsset()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var updateDto = new UpdateAssetDto
            {
                Key = "updated-key",
                Value = "updated-value",
                Description = "Updated description"
            };

            var expectedAsset = new AssetResponseDto
            {
                Id = assetId,
                Key = "updated-key",
                Value = "updated-value",
                Description = "Updated description",
                IsEncrypted = false,
                Type = AssetType.String,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedAt = DateTime.UtcNow
            };

            _mockAssetService.Setup(s => s.UpdateAssetAsync(assetId, updateDto))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _mockAssetService.Object.UpdateAssetAsync(assetId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(assetId, result.Id);
            Assert.Equal("updated-key", result.Key);
            Assert.Equal("updated-value", result.Value);
            Assert.Equal("Updated description", result.Description);
            Assert.NotNull(result.LastModifiedAt);
            _mockAssetService.Verify(s => s.UpdateAssetAsync(assetId, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAssetAsync_WithNonExistentId_ThrowsException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateAssetDto
            {
                Key = "updated-key",
                Value = "updated-value",
                Description = "Updated description"
            };

            _mockAssetService.Setup(s => s.UpdateAssetAsync(nonExistentId, updateDto))
                .ThrowsAsync(new KeyNotFoundException($"Asset with ID {nonExistentId} not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _mockAssetService.Object.UpdateAssetAsync(nonExistentId, updateDto));
            
            Assert.Contains("not found", exception.Message);
            _mockAssetService.Verify(s => s.UpdateAssetAsync(nonExistentId, updateDto), Times.Once);
        }

        #endregion

        #region DeleteAssetAsync Tests

        [Fact]
        public async Task DeleteAssetAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            _mockAssetService.Setup(s => s.DeleteAssetAsync(assetId))
                .ReturnsAsync(true);

            // Act
            var result = await _mockAssetService.Object.DeleteAssetAsync(assetId);

            // Assert
            Assert.True(result);
            _mockAssetService.Verify(s => s.DeleteAssetAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task DeleteAssetAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockAssetService.Setup(s => s.DeleteAssetAsync(invalidId))
                .ReturnsAsync(false);

            // Act
            var result = await _mockAssetService.Object.DeleteAssetAsync(invalidId);

            // Assert
            Assert.False(result);
            _mockAssetService.Verify(s => s.DeleteAssetAsync(invalidId), Times.Once);
        }

        #endregion

        #region GetAuthorizedBotAgentsAsync Tests

        [Fact]
        public async Task GetAuthorizedBotAgentsAsync_WithValidAssetId_ReturnsBotAgents()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var expectedAgents = new List<BotAgentSummaryDto>
            {
                new BotAgentSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Bot 1",
                    MachineName = "Machine-1",
                    Status = "Online"
                },
                new BotAgentSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Bot 2",
                    MachineName = "Machine-2",
                    Status = "Offline"
                }
            };

            _mockAssetService.Setup(s => s.GetAuthorizedBotAgentsAsync(assetId))
                .ReturnsAsync(expectedAgents);

            // Act
            var result = await _mockAssetService.Object.GetAuthorizedBotAgentsAsync(assetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Name == "Bot 1" && a.Status == "Online");
            Assert.Contains(result, a => a.Name == "Bot 2" && a.Status == "Offline");
            _mockAssetService.Verify(s => s.GetAuthorizedBotAgentsAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task GetAuthorizedBotAgentsAsync_WithNonExistentAssetId_ReturnsEmptyList()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockAssetService.Setup(s => s.GetAuthorizedBotAgentsAsync(nonExistentId))
                .ReturnsAsync(new List<BotAgentSummaryDto>());

            // Act
            var result = await _mockAssetService.Object.GetAuthorizedBotAgentsAsync(nonExistentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockAssetService.Verify(s => s.GetAuthorizedBotAgentsAsync(nonExistentId), Times.Once);
        }

        #endregion

        #region UpdateAuthorizedBotAgentsAsync Tests

        [Fact]
        public async Task UpdateAuthorizedBotAgentsAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var updateDto = new AssetBotAgentDto
            {
                BotAgentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            _mockAssetService.Setup(s => s.UpdateAuthorizedBotAgentsAsync(assetId, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _mockAssetService.Object.UpdateAuthorizedBotAgentsAsync(assetId, updateDto);

            // Assert
            Assert.True(result);
            _mockAssetService.Verify(s => s.UpdateAuthorizedBotAgentsAsync(assetId, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAuthorizedBotAgentsAsync_WithInvalidAssetId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new AssetBotAgentDto
            {
                BotAgentIds = new List<Guid> { Guid.NewGuid() }
            };

            _mockAssetService.Setup(s => s.UpdateAuthorizedBotAgentsAsync(invalidId, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _mockAssetService.Object.UpdateAuthorizedBotAgentsAsync(invalidId, updateDto);

            // Assert
            Assert.False(result);
            _mockAssetService.Verify(s => s.UpdateAuthorizedBotAgentsAsync(invalidId, updateDto), Times.Once);
        }

        #endregion

        #region AuthorizeBotAgentAsync Tests

        [Fact]
        public async Task AuthorizeBotAgentAsync_WithValidIds_ReturnsTrue()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();

            _mockAssetService.Setup(s => s.AuthorizeBotAgentAsync(assetId, botAgentId))
                .ReturnsAsync(true);

            // Act
            var result = await _mockAssetService.Object.AuthorizeBotAgentAsync(assetId, botAgentId);

            // Assert
            Assert.True(result);
            _mockAssetService.Verify(s => s.AuthorizeBotAgentAsync(assetId, botAgentId), Times.Once);
        }

        [Fact]
        public async Task AuthorizeBotAgentAsync_WithInvalidIds_ReturnsFalse()
        {
            // Arrange
            var invalidAssetId = Guid.NewGuid();
            var invalidBotAgentId = Guid.NewGuid();

            _mockAssetService.Setup(s => s.AuthorizeBotAgentAsync(invalidAssetId, invalidBotAgentId))
                .ReturnsAsync(false);

            // Act
            var result = await _mockAssetService.Object.AuthorizeBotAgentAsync(invalidAssetId, invalidBotAgentId);

            // Assert
            Assert.False(result);
            _mockAssetService.Verify(s => s.AuthorizeBotAgentAsync(invalidAssetId, invalidBotAgentId), Times.Once);
        }

        #endregion

        #region RevokeBotAgentAsync Tests

        [Fact]
        public async Task RevokeBotAgentAsync_WithValidIds_ReturnsTrue()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var botAgentId = Guid.NewGuid();

            _mockAssetService.Setup(s => s.RevokeBotAgentAsync(assetId, botAgentId))
                .ReturnsAsync(true);

            // Act
            var result = await _mockAssetService.Object.RevokeBotAgentAsync(assetId, botAgentId);

            // Assert
            Assert.True(result);
            _mockAssetService.Verify(s => s.RevokeBotAgentAsync(assetId, botAgentId), Times.Once);
        }

        [Fact]
        public async Task RevokeBotAgentAsync_WithInvalidIds_ReturnsFalse()
        {
            // Arrange
            var invalidAssetId = Guid.NewGuid();
            var invalidBotAgentId = Guid.NewGuid();

            _mockAssetService.Setup(s => s.RevokeBotAgentAsync(invalidAssetId, invalidBotAgentId))
                .ReturnsAsync(false);

            // Act
            var result = await _mockAssetService.Object.RevokeBotAgentAsync(invalidAssetId, invalidBotAgentId);

            // Assert
            Assert.False(result);
            _mockAssetService.Verify(s => s.RevokeBotAgentAsync(invalidAssetId, invalidBotAgentId), Times.Once);
        }

        #endregion

        #region GetAssetValueForBotAgentAsync Tests

        [Fact]
        public async Task GetAssetValueForBotAgentAsync_WithValidParameters_ReturnsValue()
        {
            // Arrange
            var assetKey = "config-key";
            var machineKey = "valid-machine-key";
            var expectedValue = "config-value";

            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync(assetKey, machineKey))
                .ReturnsAsync(expectedValue);

            // Act
            var result = await _mockAssetService.Object.GetAssetValueForBotAgentAsync(assetKey, machineKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedValue, result);
            _mockAssetService.Verify(s => s.GetAssetValueForBotAgentAsync(assetKey, machineKey), Times.Once);
        }

        [Fact]
        public async Task GetAssetValueForBotAgentAsync_WithInvalidParameters_ReturnsNull()
        {
            // Arrange
            var invalidKey = "invalid-key";
            var invalidMachineKey = "invalid-machine-key";

            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync(invalidKey, invalidMachineKey))
                .ReturnsAsync((string)null);

            // Act
            var result = await _mockAssetService.Object.GetAssetValueForBotAgentAsync(invalidKey, invalidMachineKey);

            // Assert
            Assert.Null(result);
            _mockAssetService.Verify(s => s.GetAssetValueForBotAgentAsync(invalidKey, invalidMachineKey), Times.Once);
        }

        #endregion

        #region GetAccessibleAssetsForBotAgentAsync Tests

        [Fact]
        public async Task GetAccessibleAssetsForBotAgentAsync_WithValidMachineKey_ReturnsAssets()
        {
            // Arrange
            var machineKey = "valid-machine-key";
            var expectedAssets = new List<AssetListResponseDto>
            {
                new AssetListResponseDto
                {
                    Id = Guid.NewGuid(),
                    Key = "accessible-asset-1",
                    Description = "First accessible asset",
                    Type = AssetType.String,
                    IsEncrypted = false,
                    CreatedAt = DateTime.UtcNow
                },
                new AssetListResponseDto
                {
                    Id = Guid.NewGuid(),
                    Key = "accessible-asset-2",
                    Description = "Second accessible asset",
                    Type = AssetType.Secret,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockAssetService.Setup(s => s.GetAccessibleAssetsForBotAgentAsync(machineKey))
                .ReturnsAsync(expectedAssets);

            // Act
            var result = await _mockAssetService.Object.GetAccessibleAssetsForBotAgentAsync(machineKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Key == "accessible-asset-1");
            Assert.Contains(result, a => a.Key == "accessible-asset-2");
            _mockAssetService.Verify(s => s.GetAccessibleAssetsForBotAgentAsync(machineKey), Times.Once);
        }

        [Fact]
        public async Task GetAccessibleAssetsForBotAgentAsync_WithInvalidMachineKey_ReturnsNull()
        {
            // Arrange
            var invalidMachineKey = "invalid-machine-key";

            _mockAssetService.Setup(s => s.GetAccessibleAssetsForBotAgentAsync(invalidMachineKey))
                .ReturnsAsync((IEnumerable<AssetListResponseDto>)null);

            // Act
            var result = await _mockAssetService.Object.GetAccessibleAssetsForBotAgentAsync(invalidMachineKey);

            // Assert
            Assert.Null(result);
            _mockAssetService.Verify(s => s.GetAccessibleAssetsForBotAgentAsync(invalidMachineKey), Times.Once);
        }

        #endregion
    }
} 