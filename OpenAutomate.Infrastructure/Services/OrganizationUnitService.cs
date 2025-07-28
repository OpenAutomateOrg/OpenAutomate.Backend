using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.IServices;
using OpenAutomate.Infrastructure.DbContext;
using OpenAutomate.Infrastructure.Utilities;
using Quartz;
using OpenAutomate.Infrastructure.Jobs;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationUnitService : IOrganizationUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationUnitService> _logger;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ISubscriptionService _subscriptionService;
        private readonly LemonSqueezySettings _lemonSqueezySettings;

        public OrganizationUnitService(IUnitOfWork unitOfWork, ILogger<OrganizationUnitService> logger, ISchedulerFactory schedulerFactory, ISubscriptionService subscriptionService, IOptions<LemonSqueezySettings> lemonSqueezySettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _schedulerFactory = schedulerFactory;
            _subscriptionService = subscriptionService;
            _lemonSqueezySettings = lemonSqueezySettings.Value;
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
                
                // Create default authorities for the organization unit  
                var ownerAuthority = await CreateDefaultAuthoritiesAsync(organizationUnit.Id);

                // Assign the OWNER authority to the user who CreatedAtthe organization unit
                await AssignOwnerAuthorityToUserAsync(organizationUnit.Id, userId, ownerAuthority);

                // Check if automatic trial creation is enabled
                if (_lemonSqueezySettings.EnableAutoTrialCreation)
                {
                    // Check if this is the user's first organization unit and create trial subscription
                    // This must happen BEFORE CompleteAsync to ensure it's in the same transaction
                    await CreateTrialSubscriptionForFirstOrgUnitAsync(organizationUnit.Id, userId);
                }
                
                // Commit everything in a single transaction
                await _unitOfWork.CompleteAsync();

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
            var dto = new OrganizationUnitResponseDto
            {
                Id = organizationUnit.Id,
                Name = organizationUnit.Name,
                Description = organizationUnit.Description,
                Slug = organizationUnit.Slug,
                IsActive = organizationUnit.IsActive,
                CreatedAt = organizationUnit.CreatedAt ?? DateTime.Now,
                UpdatedAt = organizationUnit.LastModifyAt,
                IsPendingDeletion = organizationUnit.ScheduledDeletionAt.HasValue,
                ScheduledDeletionAt = organizationUnit.ScheduledDeletionAt
            };

            // Calculate days until deletion if scheduled
            if (organizationUnit.ScheduledDeletionAt.HasValue)
            {
                var timeUntilDeletion = organizationUnit.ScheduledDeletionAt.Value - DateTime.UtcNow;
                dto.DaysUntilDeletion = Math.Max(0, (int)timeUntilDeletion.TotalDays);
            }

            return dto;
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

        public async Task<DeletionRequestDto> RequestDeletionAsync(Guid id, Guid userId)
        {
            try
            {
                var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
                if (organizationUnit == null)
                    throw new KeyNotFoundException($"Organization unit with ID {id} not found");

                // Check if already scheduled for deletion
                if (organizationUnit.ScheduledDeletionAt.HasValue)
                {
                    return new DeletionRequestDto
                    {
                        Success = false,
                        Message = "Organization unit is already scheduled for deletion"
                    };
                }

                // Stop all activities first
                await StopAllActivitiesAsync(id);

                // Schedule deletion for 7 days later
                var deletionDate = DateTime.UtcNow.AddDays(7);
                //var deletionDate = DateTime.UtcNow.AddMinutes(2);
                var jobId = await ScheduleDeletionJobAsync(id, deletionDate);

                // Update organization unit
                organizationUnit.IsActive = false;
                organizationUnit.ScheduledDeletionAt = deletionDate;
                organizationUnit.DeletionJobId = jobId;

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Organization unit {OrganizationUnitId} scheduled for deletion on {DeletionDate}",
                    id, deletionDate);

                return new DeletionRequestDto
                {
                    Success = true,
                    Message = "Organization unit scheduled for deletion in 7 days",
                    ScheduledDeletionAt = deletionDate,
                    DaysUntilDeletion = 7
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting deletion for organization unit {OrganizationUnitId}", id);
                throw;
            }
        }

        public async Task<DeletionRequestDto> CancelDeletionAsync(Guid id)
        {
            try
            {
                var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
                if (organizationUnit == null)
                    throw new KeyNotFoundException($"Organization unit with ID {id} not found");

                if (!organizationUnit.ScheduledDeletionAt.HasValue)
                {
                    return new DeletionRequestDto
                    {
                        Success = false,
                        Message = "Organization unit is not scheduled for deletion"
                    };
                }

                // Cancel Quartz job
                if (!string.IsNullOrEmpty(organizationUnit.DeletionJobId))
                {
                    await CancelDeletionJobAsync(organizationUnit.DeletionJobId);
                }

                // Reset organization unit
                organizationUnit.IsActive = true;
                organizationUnit.ScheduledDeletionAt = null;
                organizationUnit.DeletionJobId = null;

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Deletion cancelled for organization unit {OrganizationUnitId}", id);

                return new DeletionRequestDto
                {
                    Success = true,
                    Message = "Deletion cancelled successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deletion for organization unit {OrganizationUnitId}", id);
                throw;
            }
        }

        public async Task<DeletionStatusDto> GetDeletionStatusAsync(Guid id)
        {
            try
            {
                var organizationUnit = await _unitOfWork.OrganizationUnits.GetByIdAsync(id);
                if (organizationUnit == null)
                    throw new KeyNotFoundException($"Organization unit with ID {id} not found");

                var status = new DeletionStatusDto
                {
                    IsPendingDeletion = organizationUnit.ScheduledDeletionAt.HasValue
                };

                if (organizationUnit.ScheduledDeletionAt.HasValue)
                {
                    var timeUntilDeletion = organizationUnit.ScheduledDeletionAt.Value - DateTime.UtcNow;
                    status.ScheduledDeletionAt = organizationUnit.ScheduledDeletionAt;
                    status.DaysUntilDeletion = Math.Max(0, (int)timeUntilDeletion.TotalDays);
                    status.HoursUntilDeletion = Math.Max(0, (int)timeUntilDeletion.TotalHours);
                    status.CanCancel = timeUntilDeletion.TotalMinutes > 0;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deletion status for organization unit {OrganizationUnitId}", id);
                throw;
            }
        }

        // Helper methods
        private async Task StopAllActivitiesAsync(Guid organizationUnitId)
        {
            _logger.LogInformation("Stopping all activities for organization unit {OrganizationUnitId}", organizationUnitId);

            // Stop running executions
            var allExecutions = await _unitOfWork.Executions.GetAllAsync();
            var runningExecutions = allExecutions
                .Where(e => e.OrganizationUnitId == organizationUnitId &&
                           (e.Status == "Running" || e.Status == "Pending"))
                .ToList();

            foreach (var execution in runningExecutions)
            {
                execution.Status = "Cancelled";
                execution.EndTime = DateTime.UtcNow;
            }

            // Disable schedules
            var scheduleRepository = _unitOfWork.GetRepository<Schedule>();
            var allSchedules = await scheduleRepository.GetAllAsync();
            var activeSchedules = allSchedules
                .Where(s => s.OrganizationUnitId == organizationUnitId && s.IsEnabled)
                .ToList();

            foreach (var schedule in activeSchedules)
            {
                schedule.IsEnabled = false;
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Stopped {ExecutionCount} executions and {ScheduleCount} schedules",
                runningExecutions.Count, activeSchedules.Count);
        }

        private async Task<string> ScheduleDeletionJobAsync(Guid organizationUnitId, DateTime deletionDate)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            if (!scheduler.IsStarted)
                await scheduler.Start();

            var jobId = $"ou-deletion-{organizationUnitId}-{DateTime.UtcNow.Ticks}";
            var jobKey = new JobKey(jobId, "OU_DELETION");
            var triggerKey = new TriggerKey($"{jobId}-trigger", "OU_DELETION");

            var job = JobBuilder.Create<OrganizationUnitDeletionJob>()
                .WithIdentity(jobKey)
                .UsingJobData("OrganizationUnitId", organizationUnitId.ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .StartAt(deletionDate)
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            _logger.LogInformation("Scheduled deletion job {JobId} for organization unit {OrganizationUnitId}",
                jobId, organizationUnitId);

            return jobId;
        }

        private async Task CancelDeletionJobAsync(string jobId)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(jobId, "OU_DELETION");
                await scheduler.DeleteJob(jobKey);

                _logger.LogInformation("Cancelled deletion job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deletion job {JobId}", jobId);
            }
        }

        /// <summary>
        /// Creates a 7-day trial subscription if this is the user's first organization unit
        /// </summary>
        /// <param name="organizationUnitId">The newly created organization unit ID</param>
        /// <param name="userId">The user who created the organization unit</param>
        private async Task CreateTrialSubscriptionForFirstOrgUnitAsync(Guid organizationUnitId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Checking if user {UserId} is eligible for trial subscription", userId);

                // Check if user has any existing organization units (excluding the current one)
                var existingOrgUnits = await _unitOfWork.OrganizationUnitUsers
                    .GetAllAsync(ouu => ouu.UserId == userId && ouu.OrganizationUnitId != organizationUnitId);
                var existingOrgUnitsCount = existingOrgUnits.Count();

                if (existingOrgUnitsCount == 0)
                {
                    // This is the user's first organization unit - create trial subscription
                    var trialMinutes = _lemonSqueezySettings.TrialDurationMinutes;
                    _logger.LogInformation("Creating {TrialMinutes}-minute trial subscription for user's first organization unit {OrganizationUnitId}", trialMinutes, organizationUnitId);

                    await CreateTrialSubscriptionInternallyAsync(organizationUnitId, trialMinutes: trialMinutes);

                    _logger.LogInformation("Created trial subscription for organization unit {OrganizationUnitId}", organizationUnitId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} already has {Count} organization units - no trial subscription created", 
                        userId, existingOrgUnitsCount);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail organization unit creation
                // The organization unit should still be created successfully even if trial creation fails
                _logger.LogError(ex, "Error creating trial subscription for organization unit {OrganizationUnitId}. " +
                    "Organization unit creation will continue.", organizationUnitId);
                
                // We don't rethrow the exception to avoid failing the organization unit creation
            }
        }

        /// <summary>
        /// Creates a trial subscription internally without committing the transaction
        /// </summary>
        private async Task CreateTrialSubscriptionInternallyAsync(Guid organizationUnitId, int trialMinutes = 10080)
        {
            // Check if a subscription already exists
            var existingSubscription = await _unitOfWork.Subscriptions
                .GetAllAsync(s => s.OrganizationUnitId == organizationUnitId);

            if (existingSubscription.Any())
            {
                _logger.LogWarning("Attempted to create trial subscription for organization {OrganizationUnitId} that already has a subscription", organizationUnitId);
                return;
            }

            var subscription = new Core.Domain.Entities.Subscription
            {
                OrganizationUnitId = organizationUnitId,
                PlanName = "Premium", 
                Status = "trialing",
                TrialEndsAt = DateTime.UtcNow.AddMinutes(trialMinutes),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Subscriptions.AddAsync(subscription);
            
            _logger.LogInformation("Prepared trial subscription for organization {OrganizationUnitId} ending {TrialEndsAt}", 
                organizationUnitId, subscription.TrialEndsAt);
        }
    }
}