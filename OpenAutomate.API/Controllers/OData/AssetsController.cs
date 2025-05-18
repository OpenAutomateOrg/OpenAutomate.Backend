using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Constants;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying Assets
    /// </summary>
    /// <remarks>
    /// This controller provides OData query capabilities for Assets
    /// </remarks>
    [Route("{tenant}/odata/Assets")]
    [ApiController]
    [Authorize]
    public class AssetsController : ODataController
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(IAssetService assetService, ILogger<AssetsController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all Assets with OData query support
        /// </summary>
        /// <returns>Collection of Assets that can be queried using OData</returns>
        /// <remarks>
        /// Example queries:
        /// GET /tenant/odata/Assets?$filter=Type eq 'String'
        /// GET /tenant/odata/Assets?$orderby=CreatedAt desc&$top=10
        /// GET /tenant/odata/Assets?$select=Id,Key,Description
        /// </remarks>
        [HttpGet]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                var assets = await _assetService.GetAllAssetsAsync();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets for OData query");
                return StatusCode(500, "An error occurred while retrieving assets");
            }
        }

        /// <summary>
        /// Gets a specific Asset by ID with OData query support
        /// </summary>
        /// <param name="key">The Asset ID</param>
        /// <returns>The Asset if found</returns>
        /// <remarks>
        /// Example query:
        /// GET /tenant/odata/Assets(guid'12345678-1234-1234-1234-123456789012')?$select=Key,Description
        /// </remarks>
        [HttpGet("{key}")]
        [RequirePermission(Resources.AssetResource, Permissions.View)]
        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var asset = await _assetService.GetAssetByIdAsync(key);
                if (asset == null)
                    return NotFound();
                
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving asset with ID {AssetId} for OData query", key);
                return StatusCode(500, "An error occurred while retrieving the asset");
            }
        }
    }
}