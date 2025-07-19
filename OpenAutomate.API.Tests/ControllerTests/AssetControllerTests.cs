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
    public class AssetControllerTests
    {
        private readonly Mock<IAssetService> _mockAssetService;
        private readonly Mock<ILogger<AssetController>> _mockLogger;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly AssetController _controller;

        public AssetControllerTests()
        {
            _mockAssetService = new Mock<IAssetService>();
            _mockLogger = new Mock<ILogger<AssetController>>();
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _mockTenantContext = new Mock<ITenantContext>();
            
            _controller = new AssetController(
                _mockAssetService.Object, 
                _mockLogger.Object,
                _mockCacheInvalidationService.Object,
                _mockTenantContext.Object);
            
            // Setup controller context
            var httpContext = new DefaultHttpContext();
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.Values["tenant"] = "test-tenant";
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = routeData
            };
        }
        
        #region GetAllAssets Tests
        
        [Fact]
        public async Task GetAllAssets_ReturnsOkResult_WithListOfAssets()
        {
            // Arrange
            var assets = new List<AssetListResponseDto>
            {
                new AssetListResponseDto
                {
                    Id = Guid.NewGuid(),
                    Key = "test-asset-1",
                    Description = "Test description 1",
                    Type = AssetType.String,
                    IsEncrypted = false,
                    CreatedAt = DateTime.UtcNow,
                    AuthorizedBotAgentsCount = 1
                },
                new AssetListResponseDto
                {
                    Id = Guid.NewGuid(),
                    Key = "test-asset-2",
                    Description = "Test description 2",
                    Type = AssetType.Secret,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow,
                    AuthorizedBotAgentsCount = 2
                }
            };
            
            _mockAssetService
                .Setup(s => s.GetAllAssetsAsync())
                .ReturnsAsync(assets);
                
            // Act
            var result = await _controller.GetAllAssets();
            
            // Assert
            Assert.NotNull(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
            var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetListResponseDto>>(okResult.Value);
            Assert.Equal(2, ((List<AssetListResponseDto>)returnedAssets).Count);
        }
        
        [Fact]
        public async Task GetAllAssets_HandlesException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAssetService
                .Setup(s => s.GetAllAssetsAsync())
                .ThrowsAsync(new Exception("Test exception"));
                
            // Act
            var result = await _controller.GetAllAssets();
            
            // Assert
            Assert.NotNull(result.Result);
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }
        
        #endregion
        
        #region CreateAsset Tests
        
        [Fact]
        public async Task CreateAsset_WithValidAsset_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "new-asset",
                Value = "asset-value",
                Description = "New asset description",
                Type = AssetType.String
            };
            
            var createdAsset = new AssetResponseDto
            {
                Id = Guid.NewGuid(),
                Key = createDto.Key,
                Value = createDto.Value,
                Description = createDto.Description,
                Type = createDto.Type,
                IsEncrypted = false,
                CreatedAt = DateTime.UtcNow
            };
            
            _mockAssetService
                .Setup(s => s.CreateAssetAsync(It.IsAny<CreateAssetDto>()))
                .ReturnsAsync(createdAsset);
                
            // Act
            var result = await _controller.CreateAsset(createDto);
            
            // Assert
            Assert.NotNull(result.Result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
            
            Assert.NotNull(createdAtActionResult.Value);
            var returnedAsset = Assert.IsType<AssetResponseDto>(createdAtActionResult.Value);
            Assert.Equal(createdAsset.Id, returnedAsset.Id);
            Assert.Equal(createDto.Key, returnedAsset.Key);
        }
        
        [Fact]
        public async Task CreateAsset_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "invalid-asset",
                Value = "value"
            };
            
            _mockAssetService
                .Setup(s => s.CreateAssetAsync(It.IsAny<CreateAssetDto>()))
                .ThrowsAsync(new InvalidOperationException("Invalid asset data"));
                
            // Act
            var result = await _controller.CreateAsset(createDto);
            
            // Assert
            Assert.NotNull(result.Result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Invalid asset data", badRequestResult.Value?.ToString() ?? string.Empty);
        }
        
        [Fact]
        public async Task CreateAsset_HandlesException_ReturnsInternalServerError()
        {
            // Arrange
            var createDto = new CreateAssetDto
            {
                Key = "test-asset",
                Value = "value",
                Type = AssetType.String
            };
            
            _mockAssetService
                .Setup(s => s.CreateAssetAsync(It.IsAny<CreateAssetDto>()))
                .ThrowsAsync(new Exception("Test exception"));
                
            // Act
            var result = await _controller.CreateAsset(createDto);
            
            // Assert
            Assert.NotNull(result.Result);
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }
        
        #endregion
    }
} 