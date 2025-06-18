using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class BotAgentAssetControllerTests
    {
        private readonly Mock<IAssetService> _mockAssetService;
        private readonly Mock<ILogger<BotAgentAssetController>> _mockLogger;
        private readonly BotAgentAssetController _controller;

        public BotAgentAssetControllerTests()
        {
            _mockAssetService = new Mock<IAssetService>();
            _mockLogger = new Mock<ILogger<BotAgentAssetController>>();
            _controller = new BotAgentAssetController(_mockAssetService.Object, _mockLogger.Object);
        }

        #region GetAssetValueByKey

        [Fact]
        public async Task GetAssetValueByKey_MissingMachineKey_ReturnsUnauthorized()
        {
            // Arrange
            var request = new BotAgentAssetDto { MachineKey = null };

            // Act
            var result = await _controller.GetAssetValueByKey("asset-key", request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Machine key is required", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task GetAssetValueByKey_AssetNotFoundOrUnauthorized_ReturnsNotFound()
        {
            // Arrange
            var request = new BotAgentAssetDto { MachineKey = "machine-key" };
            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync("asset-key", "machine-key")).ReturnsAsync((string)null);

            // Act
            var result = await _controller.GetAssetValueByKey("asset-key", request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetAssetValueByKey_Success_ReturnsOk()
        {
            // Arrange
            var request = new BotAgentAssetDto { MachineKey = "machine-key" };
            var assetValue = "asset-value";
            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync("asset-key", "machine-key")).ReturnsAsync(assetValue);

            // Act
            var result = await _controller.GetAssetValueByKey("asset-key", request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(assetValue, ok.Value);
        }

        [Fact]
        public async Task GetAssetValueByKey_UnauthorizedAccessException_ReturnsForbidden()
        {
            // Arrange
            var request = new BotAgentAssetDto { MachineKey = "machine-key" };
            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync("asset-key", "machine-key"))
                .ThrowsAsync(new UnauthorizedAccessException("forbidden"));

            // Act
            var result = await _controller.GetAssetValueByKey("asset-key", request);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Contains("forbidden", forbidden.Value.ToString());
        }

        [Fact]
        public async Task GetAssetValueByKey_Exception_ReturnsServerError()
        {
            // Arrange
            var request = new BotAgentAssetDto { MachineKey = "machine-key" };
            _mockAssetService.Setup(s => s.GetAssetValueForBotAgentAsync("asset-key", "machine-key"))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _controller.GetAssetValueByKey("asset-key", request);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("error", serverError.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region GetAccessibleAssets

        [Fact]
        public async Task GetAccessibleAssets_MissingMachineKey_ReturnsUnauthorized()
        {
            // Arrange
            var request = new BotAgentKeyDto { MachineKey = null };

            // Act
            var result = await _controller.GetAccessibleAssets(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Machine key is required", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task GetAccessibleAssets_BotAgentNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new BotAgentKeyDto { MachineKey = "machine-key" };
            _mockAssetService.Setup(s => s.GetAccessibleAssetsForBotAgentAsync("machine-key")).ReturnsAsync((IEnumerable<AssetListResponseDto>)null);

            // Act
            var result = await _controller.GetAccessibleAssets(request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetAccessibleAssets_Success_ReturnsOk()
        {
            // Arrange
            var request = new BotAgentKeyDto { MachineKey = "machine-key" };
            var assets = new List<AssetListResponseDto> { new AssetListResponseDto { Key = "asset1", Description = "desc" } };
            _mockAssetService.Setup(s => s.GetAccessibleAssetsForBotAgentAsync("machine-key")).ReturnsAsync(assets);

            // Act
            var result = await _controller.GetAccessibleAssets(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(assets, ok.Value);
        }

        [Fact]
        public async Task GetAccessibleAssets_Exception_ReturnsServerError()
        {
            // Arrange
            var request = new BotAgentKeyDto { MachineKey = "machine-key" };
            _mockAssetService.Setup(s => s.GetAccessibleAssetsForBotAgentAsync("machine-key"))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _controller.GetAccessibleAssets(request);

            // Assert
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Contains("error", serverError.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}