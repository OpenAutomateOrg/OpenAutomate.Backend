using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.Utilities;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationUnitService : IOrganizationUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationUnitService> _logger;

        public OrganizationUnitService(IUnitOfWork unitOfWork, ILogger<OrganizationUnitService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OrganizationUnitResponseDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto, Guid userId)
        {
            try
            {
                // Generate slug if not provided
                string slug = !string.IsNullOrWhiteSpace(dto.Slug) 
                    ? dto.Slug 
                    : GenerateSlugFromName(dto.Name);

                // Ensure slug is unique
                slug = await EnsureUniqueSlugAsync(slug);

                // Create organization unit
                var organizationUnit = new OrganizationUnit
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Slug = slug,
                    IsActive = true
                };

                await _unitOfWork.OrganizationUnits.AddAsync(organizationUnit);
                await _unitOfWork.CompleteAsync();

                // Create default authorities for the organization unit
                await CreateDefaultAuthoritiesAsync(organizationUnit.Id);
                
                // Assign the OWNER authority to the user who CreatedAtthe organization unit
                await AssignOwnerAuthorityToUserAsync(organizationUnit.Id, userId);

                // Return response
                return new OrganizationUnitResponseDto
                {
                    Id = organizationUnit.Id,
                    Name = organizationUnit.Name,
                    Description = organizationUnit.Description,
                    Slug = organizationUnit.Slug,
                    IsActive = organizationUnit.IsActive,
                    CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                    UpdatedAt = organizationUnit.LastModifyAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization unit: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitByIdAsync(Guid id)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                return null;

            return MapToResponseDto(organizationUnit);
        }

        public async Task<OrganizationUnitResponseDto> GetOrganizationUnitBySlugAsync(string slug)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetFirstOrDefaultAsync(o => o.Slug == slug);
            if (organizationUnit == null)
                return null;

            return MapToResponseDto(organizationUnit);
        }

        public async Task<IEnumerable<OrganizationUnitResponseDto>> GetAllOrganizationUnitsAsync()
        {
            var organizationUnits = await _unitOfWork.OrganizationUnits.GetAllAsync();
            return organizationUnits.Select(MapToResponseDto);
        }

        public async Task<OrganizationUnitResponseDto> UpdateOrganizationUnitAsync(Guid id, CreateOrganizationUnitDto dto)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                throw new KeyNotFoundException($"Organization unit with ID {id} not found");

            string newSlug = dto.Slug;
            bool generateNewSlug = string.IsNullOrWhiteSpace(dto.Slug) && dto.Name != organizationUnit.Name;

            // Generate new slug if name changed and slug not explicitly provided
            if (generateNewSlug)
            {
                newSlug = GenerateSlugFromName(dto.Name);
            }

            // If slug changed, ensure it's unique
            if (newSlug != organizationUnit.Slug)
            {
                newSlug = await EnsureUniqueSlugAsync(newSlug, id);
            }

            // Update organization unit
            organizationUnit.Name = dto.Name;
            organizationUnit.Description = dto.Description;
            if (newSlug != organizationUnit.Slug)
            {
                organizationUnit.Slug = newSlug;
            }

            await _unitOfWork.CompleteAsync();

            return MapToResponseDto(organizationUnit);
        }

        public async Task<SlugChangeWarningDto> CheckNameChangeImpactAsync(Guid id, string newName)
        {
            var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
            if (organizationUnit == null)
                throw new KeyNotFoundException($"Organization unit with ID {id} not found");

            var currentSlug = organizationUnit.Slug;
            var proposedSlug = GenerateSlugFromName(newName);

            // Check if slug would change
            if (currentSlug == proposedSlug)
            {
                return new SlugChangeWarningDto
                {
                    CurrentSlug = currentSlug,
                    ProposedSlug = proposedSlug,
                    IsChangeSignificant = false,
                    PotentialImpacts = Array.Empty<string>(),
                    RequiresConfirmation = false
                };
            }

            // For a significant change with potential impact
            return new SlugChangeWarningDto
            {
                CurrentSlug = currentSlug,
                ProposedSlug = proposedSlug,
                IsChangeSignificant = true,
                PotentialImpacts = new string[]
                {
                    "URL paths will change",
                    "Bookmarks to this organization may no longer work",
                    "API integrations using the current slug will need to be updated"
                },
                RequiresConfirmation = true
            };
        }

        public string GenerateSlugFromName(string name)
        {
            return SlugGenerator.GenerateSlug(name);
        }
        
        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? excludeId = null)
        {
            bool SlugExists(string slug)
            {
                if (excludeId.HasValue)
                {
                    return _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == slug && o.Id != excludeId.Value)
                        .GetAwaiter()
                        .GetResult() != null;
                }
                else
                {
                    return _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == slug)
                        .GetAwaiter()
                        .GetResult() != null;
                }
            }
            
            return SlugGenerator.EnsureUniqueSlug(baseSlug, SlugExists);
        }

        private OrganizationUnitResponseDto MapToResponseDto(OrganizationUnit organizationUnit)
        {
            return new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                UpdatedAt = organizationUnit.LastModifyAt
            };
        }

        private async Task CreateDefaultAuthoritiesAsync(Guid organizationUnitId)
        {
            // Define the default authority names and their resource permissions
            var defaultAuthorities = new Dictionary<string, Dictionary<string, int>>
            {
                {
                    "OWNER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Delete },
                        { Resources.AgentResource, Permissions.Delete },
                        { Resources.PackageResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.UserResource, Permissions.Delete },
                        { Resources.OrganizationUnitResource, Permissions.Delete }
                    }
                },
                {
                    "OPERATOR", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Delete },
                        { Resources.AgentResource, Permissions.Delete },
                        { Resources.PackageResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.UserResource, Permissions.Update },
                        { Resources.OrganizationUnitResource, Permissions.Update }
                    }
                },
                {
                    "DEVELOPER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.Update },
                        { Resources.AgentResource, Permissions.Update },
                        { Resources.PackageResource, Permissions.Update },
                        { Resources.ScheduleResource, Permissions.Update },
                        { Resources.ExecutionResource, Permissions.Update },
                        { Resources.UserResource, Permissions.View },
                        { Resources.OrganizationUnitResource, Permissions.View }
                    }
                },
                {
                    "USER", new Dictionary<string, int>
                    {
                        { Resources.AssetResource, Permissions.View },
                        { Resources.AgentResource, Permissions.View },
                        { Resources.PackageResource, Permissions.View },
                        { Resources.ScheduleResource, Permissions.View },
                        { Resources.ExecutionResource, Permissions.View },
                        { Resources.UserResource, Permissions.View },
                        { Resources.OrganizationUnitResource, Permissions.View }
                    }
                }
            };

            foreach (var authority in defaultAuthorities)
            {
                // Create the authority
                var newAuthority = new Authority
                {
                    Name = authority.Key,
                    OrganizationUnitId = organizationUnitId
                };

                await _unitOfWork.Authorities.AddAsync(newAuthority);
                await _unitOfWork.CompleteAsync();

                // Create the resource permissions for this authority
                foreach (var resource in authority.Value)
                {
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = newAuthority.Id,
                        OrganizationUnitId = organizationUnitId,
                        ResourceName = resource.Key,
                        Permission = resource.Value
                    };

                    await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
                }
            }

            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("CreatedAtdefault authorities for organization unit {OrganizationUnitId}", organizationUnitId);
        }

        private async Task AssignOwnerAuthorityToUserAsync(Guid organizationUnitId, Guid userId)
        {
            try
            {
                // Find the OWNER authority for this organization unit
                var ownerAuthority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                    a => a.OrganizationUnitId == organizationUnitId && a.Name == "OWNER");
                
                if (ownerAuthority == null)
                {
                    _logger.LogError("OWNER authority not found for organization unit {OrganizationUnitId}", organizationUnitId);
                    throw new InvalidOperationException($"OWNER authority not found for organization unit {organizationUnitId}");
                }
                
                // Create the user-authority association
                var userAuthority = new UserAuthority
                {
                    UserId = userId,
                    AuthorityId = ownerAuthority.Id,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
                await _unitOfWork.CompleteAsync();
                
                _logger.LogInformation("User {UserId} assigned as OWNER of organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning OWNER authority to user {UserId} for organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
                throw;
            }
        }

        public async Task<IEnumerable<OrganizationUnitResponseDto>> GetOrganizationUnitsByUserIdAsync(Guid userId)
        {
            try
            {
                var organizationUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllAsync(
                    ouu => ouu.UserId == userId,
                    null,
                    ouu => ouu.OrganizationUnit // include navigation property
                );

                var organizationUnits = organizationUnitUsers
                    .Select(ouu => ouu.OrganizationUnit)
                    .ToList();

                return organizationUnits.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization units for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }
    }
} 