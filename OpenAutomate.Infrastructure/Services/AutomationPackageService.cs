using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the automation package service
    /// </summary>
    public class AutomationPackageService : IAutomationPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly IPackageStorageService _storageService;
        private readonly AwsSettings _awsSettings;
        private readonly ILogger<AutomationPackageService> _logger;

        public AutomationPackageService(
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext,
            IPackageStorageService storageService,
            IOptions<AwsSettings> awsSettings,
            ILogger<AutomationPackageService> logger)
        {
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
            _storageService = storageService;
            _awsSettings = awsSettings.Value;
            _logger = logger;
        }

        public async Task<AutomationPackageResponseDto> CreatePackageAsync(CreateAutomationPackageDto dto)
        {
            // Check if package name already exists
            var existingPackage = await _unitOfWork.AutomationPackages.GetFirstOrDefaultAsync(
                p => p.Name == dto.Name && p.OrganizationUnitId == _tenantContext.CurrentTenantId);

            if (existingPackage != null)
            {
                throw new InvalidOperationException($"Package with name '{dto.Name}' already exists");
            }

            var package = new AutomationPackage
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true,
                OrganizationUnitId = _tenantContext.CurrentTenantId
            };

            await _unitOfWork.AutomationPackages.AddAsync(package);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Automation package created: {PackageId}, Name: {Name}", 
                package.Id, package.Name);

            return MapToResponseDto(package);
        }

        public async Task<AutomationPackageResponseDto?> GetPackageByIdAsync(Guid id)
        {
            var package = await _unitOfWork.AutomationPackages.GetByIdAsync(id);
            if (package == null || package.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                return null;
            }

            // Load versions
            var versions = await _unitOfWork.PackageVersions.GetAllAsync(
                pv => pv.PackageId == id && pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            var responseDto = MapToResponseDto(package);
            responseDto.Versions = versions.Select(MapVersionToResponseDto).ToList();

            return responseDto;
        }

        public async Task<IEnumerable<AutomationPackageResponseDto>> GetAllPackagesAsync()
        {
            var packages = await _unitOfWork.AutomationPackages.GetAllAsync(
                p => p.OrganizationUnitId == _tenantContext.CurrentTenantId);

            var responseDtos = new List<AutomationPackageResponseDto>();

            foreach (var package in packages)
            {
                var responseDto = MapToResponseDto(package);
                
                // Load versions for each package
                var versions = await _unitOfWork.PackageVersions.GetAllAsync(
                    pv => pv.PackageId == package.Id && pv.OrganizationUnitId == _tenantContext.CurrentTenantId);
                
                responseDto.Versions = versions.Select(MapVersionToResponseDto).ToList();
                responseDtos.Add(responseDto);
            }

            return responseDtos;
        }

        public async Task<PackageVersionResponseDto> UploadPackageVersionAsync(
            Guid packageId, Stream fileStream, string fileName, string version)
        {
            // Verify package exists and belongs to current tenant
            var package = await _unitOfWork.AutomationPackages.GetByIdAsync(packageId);
            if (package == null || package.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ArgumentException("Package not found");
            }

            // Check if version already exists
            var existingVersion = await _unitOfWork.PackageVersions.GetFirstOrDefaultAsync(
                pv => pv.PackageId == packageId && 
                      pv.VersionNumber == version && 
                      pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            if (existingVersion != null)
            {
                throw new InvalidOperationException($"Version {version} already exists for this package");
            }

            // Generate unique object key for S3
            var objectKey = GenerateObjectKey(packageId, version, fileName);

            // Get file size
            var fileSize = fileStream.Length;

            // Upload to S3
            await _storageService.UploadAsync(fileStream, objectKey);

            // Create package version record
            var packageVersion = new PackageVersion
            {
                PackageId = packageId,
                VersionNumber = version,
                FilePath = objectKey,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = GetContentType(fileName),
                IsActive = true,
                UploadedAt = DateTime.UtcNow,
                OrganizationUnitId = _tenantContext.CurrentTenantId
            };

            await _unitOfWork.PackageVersions.AddAsync(packageVersion);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Package version uploaded: {PackageId}, Version: {Version}, Size: {FileSize}", 
                packageId, version, fileSize);

            return MapVersionToResponseDto(packageVersion);
        }

        public async Task<string> GetPackageDownloadUrlAsync(Guid packageId, string version)
        {
            // Verify package exists and belongs to current tenant
            var package = await _unitOfWork.AutomationPackages.GetByIdAsync(packageId);
            if (package == null || package.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ArgumentException("Package not found");
            }

            // Find the specific version
            var packageVersion = await _unitOfWork.PackageVersions.GetFirstOrDefaultAsync(
                pv => pv.PackageId == packageId && 
                      pv.VersionNumber == version && 
                      pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            if (packageVersion == null)
            {
                throw new ArgumentException($"Version {version} not found for this package");
            }

            // Generate presigned URL
            var expiresIn = TimeSpan.FromMinutes(_awsSettings.PresignedUrlExpirationMinutes);
            var downloadUrl = await _storageService.GetDownloadUrlAsync(packageVersion.FilePath, expiresIn);

            _logger.LogInformation("Generated download URL for package: {PackageId}, Version: {Version}", 
                packageId, version);

            return downloadUrl;
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var package = await _unitOfWork.AutomationPackages.GetByIdAsync(id);
            if (package == null || package.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ArgumentException("Package not found");
            }

            // Get all versions to delete from S3
            var versions = await _unitOfWork.PackageVersions.GetAllAsync(
                pv => pv.PackageId == id && pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            // Delete files from S3
            foreach (var version in versions)
            {
                try
                {
                    await _storageService.DeleteAsync(version.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete S3 object: {ObjectKey}", version.FilePath);
                }
            }

            // Delete from database
            _unitOfWork.AutomationPackages.Remove(package);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Package deleted: {PackageId}", id);
        }

        public async Task DeletePackageVersionAsync(Guid packageId, string version)
        {
            // Verify package exists and belongs to current tenant
            var package = await _unitOfWork.AutomationPackages.GetByIdAsync(packageId);
            if (package == null || package.OrganizationUnitId != _tenantContext.CurrentTenantId)
            {
                throw new ArgumentException("Package not found");
            }

            // Find the specific version
            var packageVersion = await _unitOfWork.PackageVersions.GetFirstOrDefaultAsync(
                pv => pv.PackageId == packageId && 
                      pv.VersionNumber == version && 
                      pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            if (packageVersion == null)
            {
                throw new ArgumentException($"Version {version} not found for this package");
            }

            // Delete from S3
            try
            {
                await _storageService.DeleteAsync(packageVersion.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete S3 object: {ObjectKey}", packageVersion.FilePath);
            }

            // Delete from database
            _unitOfWork.PackageVersions.Remove(packageVersion);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Package version deleted: {PackageId}, Version: {Version}", 
                packageId, version);
        }

        private string GenerateObjectKey(Guid packageId, string version, string fileName)
        {
            // Create unique object key: packages/{packageId}/{version}/{fileName}
            return $"packages/{packageId}/{version}/{fileName}";
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".zip" => "application/zip",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".7z" => "application/x-7z-compressed",
                _ => "application/octet-stream"
            };
        }

        private AutomationPackageResponseDto MapToResponseDto(AutomationPackage package)
        {
            return new AutomationPackageResponseDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt ?? DateTime.UtcNow
            };
        }

        private PackageVersionResponseDto MapVersionToResponseDto(PackageVersion version)
        {
            return new PackageVersionResponseDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                FileName = version.FileName,
                FileSize = version.FileSize,
                ContentType = version.ContentType,
                IsActive = version.IsActive,
                UploadedAt = version.UploadedAt
            };
        }

        /// <summary>
        /// Checks if a package with the given name and version already exists
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="version">The version number</param>
        /// <returns>True if the combination exists, false otherwise</returns>
        public async Task<bool> PackageVersionExistsAsync(string packageName, string version)
        {
            var package = await _unitOfWork.AutomationPackages.GetFirstOrDefaultAsync(
                p => p.Name == packageName && p.OrganizationUnitId == _tenantContext.CurrentTenantId);

            if (package == null)
            {
                return false;
            }

            var existingVersion = await _unitOfWork.PackageVersions.GetFirstOrDefaultAsync(
                pv => pv.PackageId == package.Id && 
                      pv.VersionNumber == version && 
                      pv.OrganizationUnitId == _tenantContext.CurrentTenantId);

            return existingVersion != null;
        }
    }
} 