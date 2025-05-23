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
                _logger.LogInformation("Starting organization unit creation for user {UserId} with name '{Name}'", userId, dto.Name);

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
                _logger.LogInformation("Added organization unit {OrganizationUnitId} to unit of work", organizationUnit.Id);

                // Create default authorities for the organization unit (but don't commit yet)
                var ownerAuthority = await CreateDefaultAuthoritiesInternalAsync(organizationUnit.Id);
                _logger.LogInformation("Created authorities for organization unit {OrganizationUnitId}, OWNER authority ID: {AuthorityId}", 
                    organizationUnit.Id, ownerAuthority.Id);
                
                // Assign the OWNER authority to the user who created the organization unit (but don't commit yet)
                await AssignOwnerAuthorityInternalAsync(organizationUnit.Id, userId, ownerAuthority);

                // Commit everything in a single transaction
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Successfully created organization unit {OrganizationUnitId} with user {UserId} as OWNER", 
                    organizationUnit.Id, userId);

                // Verify the user association was created by querying it
                var userOrgUnits = await _unitOfWork.OrganizationUnitUsers.GetAllAsync(ou => ou.UserId == userId);
                _logger.LogInformation("User {UserId} now belongs to {Count} organization units", userId, userOrgUnits.Count());

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
                _logger.LogInformation("Retrieving organization units for user {UserId}", userId);

                // Get all organization unit users for this user
                var organizationUnitUsers = await _unitOfWork.OrganizationUnitUsers
                    .GetAllAsync(ou => ou.UserId == userId);
                
                _logger.LogInformation("Found {Count} OrganizationUnitUser records for user {UserId}", 
                    organizationUnitUsers.Count(), userId);

                // Extract the organization unit IDs
                var organizationUnitIds = organizationUnitUsers.Select(ou => ou.OrganizationUnitId).ToList();
                
                _logger.LogInformation("Organization unit IDs for user {UserId}: [{OrganizationUnitIds}]", 
                    userId, string.Join(", ", organizationUnitIds));

                // Get the actual organization units
                var organizationUnits = new List<OrganizationUnit>();
                
                foreach (var ouId in organizationUnitIds)
                {
                    var orgUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(ouId);
                    if (orgUnit != null)
                    {
                        organizationUnits.Add(orgUnit);
                        _logger.LogDebug("Found organization unit {OrganizationUnitId} with name '{Name}'", ouId, orgUnit.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Organization unit {OrganizationUnitId} not found in database", ouId);
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

        private async Task<Authority> CreateDefaultAuthoritiesInternalAsync(Guid organizationUnitId)
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

            // Create all authorities
            var createdAuthorities = new List<Authority>();
            foreach (var authority in defaultAuthorities)
            {
                var newAuthority = new Authority
                {
                    Name = authority.Key,
                    OrganizationUnitId = organizationUnitId
                };

                await _unitOfWork.Authorities.AddAsync(newAuthority);
                createdAuthorities.Add(newAuthority);
            }

            // Create all authority resources
            foreach (var authorityData in defaultAuthorities)
            {
                var authority = createdAuthorities.First(a => a.Name == authorityData.Key);
                
                foreach (var resource in authorityData.Value)
                {
                    var authorityResource = new AuthorityResource
                    {
                        AuthorityId = authority.Id,
                        OrganizationUnitId = organizationUnitId,
                        ResourceName = resource.Key,
                        Permission = resource.Value
                    };

                    await _unitOfWork.AuthorityResources.AddAsync(authorityResource);
                }
            }

            _logger.LogInformation("Prepared default authorities for organization unit {OrganizationUnitId}", organizationUnitId);

            return createdAuthorities.First(a => a.Name == "OWNER");
        }

        private async Task AssignOwnerAuthorityInternalAsync(Guid organizationUnitId, Guid userId, Authority ownerAuthority)
        {
            try
            {
                _logger.LogInformation("Assigning OWNER authority {AuthorityId} to user {UserId} for organization unit {OrganizationUnitId}", 
                    ownerAuthority.Id, userId, organizationUnitId);

                // Create the user-authority association
                var userAuthority = new UserAuthority
                {
                    UserId = userId,
                    AuthorityId = ownerAuthority.Id,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.UserAuthorities.AddAsync(userAuthority);
                _logger.LogInformation("Added UserAuthority record: UserId={UserId}, AuthorityId={AuthorityId}, OrganizationUnitId={OrganizationUnitId}", 
                    userId, ownerAuthority.Id, organizationUnitId);
                
                // Create the direct user-organization association
                var organizationUnitUser = new OrganizationUnitUser
                {
                    UserId = userId,
                    OrganizationUnitId = organizationUnitId
                };
                
                await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
                _logger.LogInformation("Added OrganizationUnitUser record: UserId={UserId}, OrganizationUnitId={OrganizationUnitId}", 
                    userId, organizationUnitId);
                
                _logger.LogInformation("Prepared user {UserId} as OWNER of organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing OWNER authority assignment for user {UserId} and organization unit {OrganizationUnitId}", 
                    userId, organizationUnitId);
                throw;
            }
        }
    }
} 