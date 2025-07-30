using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;
using static OpenAutomate.API.Attributes.RequireSubscriptionAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for Automation Package management
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/packages")]
    [Authorize]
    public class AutomationPackageController : ControllerBase
    {
        private readonly IAutomationPackageService _packageService;
        private readonly IPackageMetadataService _metadataService;
        private readonly IBotAgentService _botAgentService;
        private readonly ILogger<AutomationPackageController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationPackageController"/> class
        /// </summary>
        /// <param name="packageService">The automation package service</param>
        /// <param name="metadataService">The package metadata service</param>
        /// <param name="botAgentService">The bot agent service</param>
        /// <param name="logger">The logger</param>
        public AutomationPackageController(
            IAutomationPackageService packageService,
            IPackageMetadataService metadataService,
            IBotAgentService botAgentService,
            ILogger<AutomationPackageController> logger)
        {
            _packageService = packageService;
            _metadataService = metadataService;
            _botAgentService = botAgentService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new automation package
        /// </summary>
        /// <param name="dto">Package creation data</param>
        /// <returns>Created package response</returns>
        [HttpPost]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Create)]
        public async Task<ActionResult<AutomationPackageResponseDto>> CreatePackage([FromBody] CreateAutomationPackageDto dto)
        {
            var package = await _packageService.CreatePackageAsync(dto);
            
            // Get the tenant from the route data
            var tenant = RouteData.Values["tenant"]?.ToString();
            
            return CreatedAtAction(
                nameof(GetPackageById), 
                new { tenant = tenant, id = package.Id }, 
                package);
        }

        /// <summary>
        /// Uploads a package file and automatically creates the package with extracted metadata
        /// </summary>
        /// <param name="request">Upload request containing the package file</param>
        /// <returns>Created package with first version</returns>
        [HttpPost("upload")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Create)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AutomationPackageResponseDto>> UploadPackageWithAutoCreation(
            [FromForm] UploadPackageWithMetadataRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is required");

            try
            {
                using var stream = request.File.OpenReadStream();
                
                // First, validate it's a valid package
                var isValid = await _metadataService.IsValidPackageAsync(stream, request.File.FileName);
                if (!isValid)
                {
                    return BadRequest("Invalid package file. Must be a ZIP file containing a bot.py file.");
                }

                // Extract metadata from the package
                var metadata = await _metadataService.ExtractMetadataAsync(stream, request.File.FileName);
                if (!metadata.IsValid)
                {
                    return BadRequest($"Failed to extract package metadata: {metadata.ErrorMessage}");
                }

                // Allow manual override of extracted metadata
                var packageName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : metadata.Name;
                var packageDescription = !string.IsNullOrWhiteSpace(request.Description) ? request.Description : metadata.Description;
                var versionNumber = !string.IsNullOrWhiteSpace(request.Version) ? request.Version : metadata.Version;

                // Check if package name and version combination already exists
                var exists = await _packageService.PackageVersionExistsAsync(packageName, versionNumber);
                if (exists)
                {
                    return Conflict(new { 
                        error = $"Package '{packageName}' version '{versionNumber}' already exists. Please increment the version number." 
                    });
                }

                // Check if a package with this name already exists
                var existingPackage = await _packageService.GetPackageByNameAsync(packageName);
                
                AutomationPackageResponseDto package;
                if (existingPackage != null)
                {
                    // Package exists, use the existing one
                    package = existingPackage;
                }
                else
                {
                    // Create new package
                    var createDto = new CreateAutomationPackageDto
                    {
                        Name = packageName,
                        Description = packageDescription
                    };
                    package = await _packageService.CreatePackageAsync(createDto);
                }

                // Upload the version to the package (existing or new)
                stream.Position = 0; // Reset stream for upload
                var packageVersion = await _packageService.UploadPackageVersionAsync(
                    package.Id, stream, request.File.FileName, versionNumber);

                // Get the complete package with versions
                var completePackage = await _packageService.GetPackageByIdAsync(package.Id);
                
                // Get the tenant from the route data
                var tenant = RouteData.Values["tenant"]?.ToString();
                
                return CreatedAtAction(
                    nameof(GetPackageById), 
                    new { tenant = tenant, id = package.Id }, 
                    completePackage);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a package by ID
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Package response</returns>
        [HttpGet("{id}")]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.PackageResource, Permissions.View)]
        public async Task<ActionResult<AutomationPackageResponseDto>> GetPackageById(Guid id)
        {
            var package = await _packageService.GetPackageByIdAsync(id);
            if (package == null)
                return NotFound();

            return Ok(package);
        }

        /// <summary>
        /// Gets all packages for the current tenant
        /// </summary>
        /// <returns>Collection of package responses</returns>
        [HttpGet]
        [RequireSubscription(SubscriptionOperationType.Read)]
        [RequirePermission(Resources.PackageResource, Permissions.View)]
        public async Task<ActionResult<IEnumerable<AutomationPackageResponseDto>>> GetAllPackages()
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return Ok(packages);
        }

        /// <summary>
        /// Uploads a new version of a package
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="request">Upload request containing file and version</param>
        /// <returns>Package version response</returns>
        [HttpPost("{id}/versions")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Update)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<PackageVersionResponseDto>> UploadPackageVersion(
            Guid id,
            [FromForm] UploadPackageVersionRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is required");

            if (string.IsNullOrWhiteSpace(request.Version))
                return BadRequest("Version is required");

            try
            {
                using var stream = request.File.OpenReadStream();
                var packageVersion = await _packageService.UploadPackageVersionAsync(
                    id, stream, request.File.FileName, request.Version);
                
                // Get the tenant from the route data
                var tenant = RouteData.Values["tenant"]?.ToString();
                
                return CreatedAtAction(
                    nameof(GetPackageById), 
                    new { tenant = tenant, id }, 
                    packageVersion);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a download URL for a specific package version
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="version">Version number</param>
        /// <returns>Download URL response</returns>
        [HttpGet("{id}/versions/{version}/download")]
        [RequirePermission(Resources.PackageResource, Permissions.View)]
        public async Task<ActionResult<object>> GetPackageDownloadUrl(Guid id, string version)
        {
            try
            {
                var downloadUrl = await _packageService.GetPackageDownloadUrlAsync(id, version);
                return Ok(new { downloadUrl });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Gets a secure download URL for a package version (for bot agents)
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="version">Version number</param>
        /// <param name="machineKey">Bot agent machine key for authentication</param>
        /// <returns>Download URL response with expiration</returns>
        [HttpGet("{id}/versions/{version}/agent-download")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Bot agents use machine key auth
        public async Task<IActionResult> GetAgentDownloadUrl(Guid id, string version, [FromQuery] string machineKey)
        {
            try
            {
                // Validate machine key
                var botAgents = await _botAgentService.GetAllBotAgentsAsync();
                var botAgent = botAgents.FirstOrDefault(ba => ba.MachineKey == machineKey);
                if (botAgent == null)
                    return Unauthorized("Invalid machine key");

                // Get download URL
                var downloadUrl = await _packageService.GetPackageDownloadUrlAsync(id, version);
                
                return Ok(new { 
                    downloadUrl,
                    packageId = id,
                    version,
                    expiresAt = DateTime.UtcNow.AddHours(1) // URL expires in 1 hour
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent download URL");
                return StatusCode(500, "Error getting download URL");
            }
        }

        /// <summary>
        /// Deletes a package and all its versions
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>No content response</returns>
        [HttpDelete("{id}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Delete)]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            try
            {
                await _packageService.DeletePackageAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a specific package version
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="version">Version number</param>
        /// <returns>No content response</returns>
        [HttpDelete("{id}/versions/{version}")]
        [RequireSubscription(SubscriptionOperationType.Write)]
        [RequirePermission(Resources.PackageResource, Permissions.Delete)]
        public async Task<IActionResult> DeletePackageVersion(Guid id, string version)
        {
            try
            {
                await _packageService.DeletePackageVersionAsync(id, version);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for uploading package versions
    /// </summary>
    public class UploadPackageVersionRequest
    {
        /// <summary>
        /// Package file to upload
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Version number for the package
        /// </summary>
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for uploading packages with automatic metadata extraction
    /// </summary>
    public class UploadPackageWithMetadataRequest
    {
        /// <summary>
        /// Package file to upload
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Optional: Override the extracted package name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Optional: Override the extracted package description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional: Override the extracted package version
        /// </summary>
        public string? Version { get; set; }
    }
} 