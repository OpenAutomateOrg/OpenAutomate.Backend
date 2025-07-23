using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.DbContext;
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
                // Generate slug from name
                string slug = GenerateSlugFromName(dto.Name);

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
                var ownerAuthority = await CreateDefaultAuthoritiesAsync(organizationUnit.Id);
                
                // Assign the OWNER authority to the user who CreatedAtthe organization unit
                await AssignOwnerAuthorityToUserAsync(organizationUnit.Id, userId, ownerAuthority);

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

            // Generate new slug if name changed
            string newSlug = organizationUnit.Slug;
            if (dto.Name != organizationUnit.Name)
            {
                newSlug = GenerateSlugFromName(dto.Name);
                // Ensure new slug is unique
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
        
        /// <summary>
        /// Gets all organization units that a user belongs to, regardless of role
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A response containing organization units and the total count</returns>
        public async Task<UserOrganizationUnitsResponseDto> GetUserOrganizationUnitsAsync(Guid userId)
        {
            try
            {
                // Get all organization unit users for this user
                // NOTE: We bypass tenant filtering here because this endpoint is specifically designed
                // to discover which organization units a user belongs to across all tenants
                var organizationUnitUsers = await _unitOfWork.OrganizationUnitUsers.GetAllIgnoringFiltersAsync(ou => ou.UserId == userId);
                
                // Extract the organization unit IDs
                var organizationUnitIds = organizationUnitUsers.Select(ou => ou.OrganizationUnitId).ToList();
                
                // Get the actual organization units
                var organizationUnits = new List<OrganizationUnit>();
                
                foreach (var ouId in organizationUnitIds)
                {
                    var orgUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(ouId);
                    if (orgUnit != null)
                    {
                        organizationUnits.Add(orgUnit);
                    }
                }
                
                // Map to DTOs
                var organizationUnitDtos = organizationUnits.Select(MapToResponseDto).ToList();
                
                // Create the response DTO
                var response = new UserOrganizationUnitsResponseDto
                {
                    Count = organizationUnitDtos.Count,
                    OrganizationUnits = organizationUnitDtos
                };
                
                _logger.LogInformation("Retrieved {Count} organization units for user {UserId}", 
                    response.Count, userId);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization units for user {UserId}: {Message}", 
                    userId, ex.Message);
                throw;
            }
        }
        
        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? excludeId = null)
        {
            // Check if base slug is unique
            bool baseSlugExists;
            if (excludeId.HasValue)
            {
                var result = await _unitOfWork.OrganizationUnits
                    .GetFirstOrDefaultAsync(o => o.Slug == baseSlug && o.Id != excludeId.Value);
                baseSlugExists = result != null;
            }
            else
            {
                var result = await _unitOfWork.OrganizationUnits
                    .GetFirstOrDefaultAsync(o => o.Slug == baseSlug);
                baseSlugExists = result != null;
            }

            if (!baseSlugExists)
                return baseSlug;

            // Find unique slug with counter
            int counter = 2;
            string newSlug;
            bool slugExists;

            do
            {
                newSlug = $"{baseSlug}-{counter}";
                
                if (excludeId.HasValue)
                {
                    var result = await _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == newSlug && o.Id != excludeId.Value);
                    slugExists = result != null;
                }
                else
                {
                    var result = await _unitOfWork.OrganizationUnits
                        .GetFirstOrDefaultAsync(o => o.Slug == newSlug);
                    slugExists = result != null;
                }
                
                counter++;
            } while (slugExists);

            return newSlug;
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

        private async Task<Authority> CreateDefaultAuthoritiesAsync(Guid organizationUnitId)
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
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
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
                        { Resources.ExecutionResource, Permissions.Delete },
                        { Resources.ScheduleResource, Permissions.Delete },
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
                        { Resources.ExecutionResource, Permissions.Update },
                        { Resources.ScheduleResource, Permissions.Update },
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
                        { Resources.ExecutionResource, Permissions.View },
                        { Resources.ScheduleResource, Permissions.View },
                        { Resources.UserResource, Permissions.View },
                        { Resources.OrganizationUnitResource, Permissions.View }
                    }
                }
            };

            Authority ownerAuthority = null;

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

                // Keep reference to the OWNER authority
                if (authority.Key == "OWNER")
                {
                    ownerAuthority = newAuthority;
                }

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
            _logger.LogInformation("Created default authorities for organization unit {OrganizationUnitId}", organizationUnitId);
            
            return ownerAuthority;
        }

        private async Task AssignOwnerAuthorityToUserAsync(Guid organizationUnitId, Guid userId, Authority ownerAuthority)
        {
            try
            {
                if (ownerAuthority == null)
                {
                    _logger.LogError("OWNER authority is null for organization unit {OrganizationUnitId}", organizationUnitId);
                    throw new InvalidOperationException($"OWNER authority is null for organization unit {organizationUnitId}");
                }
                
                // Create the user-authority association
                var userAuthority = new UserAuthority
                {
                    UserId = userId,
                    AuthorityId = ownerAuthority.Id,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
                
                // Create the direct user-organization association
                var organizationUnitUser = new OrganizationUnitUser
                {
                    UserId = userId,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
                await _unitOfWork.CompleteAsync();
                
                _logger.LogInformation("User {UserId} assigned as OWNER of organization unit {OrganizationUnitId} and added to organization", 
                    userId, organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning OWNER authority to user {UserId} for organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
                throw;
            }
        }
    }
} 