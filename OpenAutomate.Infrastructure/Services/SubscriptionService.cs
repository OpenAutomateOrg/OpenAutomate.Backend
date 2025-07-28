using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for managing subscription lifecycle and validation
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly LemonSqueezySettings _lemonSqueezySettings;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            ILogger<SubscriptionService> logger,
            IOptions<LemonSqueezySettings> lemonSqueezySettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _lemonSqueezySettings = lemonSqueezySettings.Value;
        }

        public async Task<Subscription?> GetCurrentSubscriptionAsync(Guid organizationUnitId)
        {
            try
            {
                // Get the most recent subscription for the organization
                var subscription = await _unitOfWork.Subscriptions
                    .GetAllAsync(s => s.OrganizationUnitId == organizationUnitId);

                return subscription.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current subscription for organization {OrganizationUnitId}", organizationUnitId);
                throw;
            }
        }

        public async Task<bool> HasActiveSubscriptionAsync(Guid organizationUnitId)
        {
            try
            {
                var subscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                
                if (subscription == null)
                    return false;

                // Check if trial has expired
                if (subscription.Status == "trialing" && subscription.TrialEndsAt.HasValue && subscription.TrialEndsAt.Value < DateTime.UtcNow)
                {
                    return false; // Trial has expired
                }

                // Check if subscription is active (trial or paid)
                return subscription.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active subscription for organization {OrganizationUnitId}", organizationUnitId);
                return false; // Fail safe - deny access if we can't determine status
            }
        }

        public async Task<Subscription> CreateTrialSubscriptionAsync(Guid organizationUnitId, int trialDays = 7)
        {
            try
            {
                // Check if a subscription already exists
                var existingSubscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                if (existingSubscription != null)
                {
                    _logger.LogWarning("Attempted to create trial subscription for organization {OrganizationUnitId} that already has a subscription", organizationUnitId);
                    return existingSubscription;
                }

                var subscription = new Subscription
                {
                    OrganizationUnitId = organizationUnitId,
                    PlanName = "Premium",
                    Status = "trialing",
                    TrialEndsAt = DateTime.UtcNow.AddDays(trialDays),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Subscriptions.AddAsync(subscription);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Created trial subscription for organization {OrganizationUnitId} ending {TrialEndsAt}", 
                    organizationUnitId, subscription.TrialEndsAt);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trial subscription for organization {OrganizationUnitId}", organizationUnitId);
                throw;
            }
        }

        public async Task<bool> StartTrialSubscriptionAsync(Guid organizationUnitId, string userId)
        {
            try
            {
                // Parse userId to Guid
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                    return false;
                }

                // Check if a subscription already exists for this organization
                var existingSubscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                if (existingSubscription != null)
                {
                    _logger.LogWarning("Attempted to create trial subscription for organization {OrganizationUnitId} that already has a subscription", organizationUnitId);
                    return false;
                }

                // Check if this user has already used a trial on any organization unit (cross-tenant check)
                // Check for ANY past trial subscription regardless of current status (trialing, expired, active, etc.)
                var userTrialSubscriptions = await _unitOfWork.Subscriptions
                    .GetAllIgnoringFiltersAsync(s => s.CreatedBy == userGuid && s.TrialEndsAt != null);
                
                if (userTrialSubscriptions.Any())
                {
                    _logger.LogWarning("User {UserId} attempted to create trial subscription but already has used a trial (found {TrialCount} past trials)", 
                        userId, userTrialSubscriptions.Count());
                    return false;
                }

                // Check if current organization unit is the user's first organization unit
                var userOrganizationUnits = await _unitOfWork.OrganizationUnits
                    .GetAllIgnoringFiltersAsync(ou => ou.CreatedBy == userGuid);
                
                if (!userOrganizationUnits.Any())
                {
                    _logger.LogWarning("No organization units found for user {UserId}", userId);
                    return false;
                }

                // Find the first (earliest) organization unit created by this user
                // Handle nullable CreatedAt - units without CreatedAt go to end, then order by actual dates
                var firstOrganizationUnit = userOrganizationUnits
                    .Where(ou => ou.CreatedAt.HasValue) // Only consider units with valid CreatedAt
                    .OrderBy(ou => ou.CreatedAt.Value)
                    .FirstOrDefault();

                // If no units have CreatedAt, fall back to ordering by ID (which is always set)
                if (firstOrganizationUnit == null)
                {
                    firstOrganizationUnit = userOrganizationUnits
                        .OrderBy(ou => ou.Id)
                        .First();
                    _logger.LogWarning("No organization units with CreatedAt found for user {UserId}, using ID-based ordering", userId);
                }

                // Only allow trial on the first organization unit
                if (firstOrganizationUnit.Id != organizationUnitId)
                {
                    _logger.LogWarning("User {UserId} attempted to create trial on organization {OrganizationUnitId} but trial is only allowed on first organization unit {FirstOrganizationUnitId}", 
                        userId, organizationUnitId, firstOrganizationUnit.Id);
                    return false;
                }

                var trialMinutes = _lemonSqueezySettings.TrialDurationMinutes;
                var subscription = new Subscription
                {
                    OrganizationUnitId = organizationUnitId,
                    PlanName = "Premium",
                    Status = "trialing",
                    TrialEndsAt = DateTime.UtcNow.AddMinutes(trialMinutes),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userGuid
                };

                await _unitOfWork.Subscriptions.AddAsync(subscription);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Created {TrialMinutes}-minute trial subscription for organization {OrganizationUnitId} by user {UserId} ending {TrialEndsAt}", 
                    trialMinutes, organizationUnitId, userId, subscription.TrialEndsAt);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trial subscription for organization {OrganizationUnitId} by user {UserId}", organizationUnitId, userId);
                throw;
            }
        }

        public async Task<Subscription> UpdateSubscriptionFromWebhookAsync(
            string lemonsqueezySubscriptionId,
            string status,
            DateTime? renewsAt,
            DateTime? endsAt,
            Guid organizationUnitId)
        {
            try
            {
                // Try to find existing subscription by Lemon Squeezy ID
                var existingSubscriptions = await _unitOfWork.Subscriptions
                    .GetAllAsync(s => s.LemonsqueezySubscriptionId == lemonsqueezySubscriptionId);
                
                var subscription = existingSubscriptions.FirstOrDefault();

                if (subscription == null)
                {
                    // Try to find by organization unit ID and update it
                    subscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                    
                    if (subscription == null)
                    {
                        // Create new subscription
                        subscription = new Subscription
                        {
                            OrganizationUnitId = organizationUnitId,
                            PlanName = "Premium",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Subscriptions.AddAsync(subscription);
                    }
                }

                // Update subscription details
                subscription.LemonsqueezySubscriptionId = lemonsqueezySubscriptionId;
                subscription.Status = status;
                subscription.RenewsAt = renewsAt;
                subscription.EndsAt = endsAt;
                subscription.LastModifyAt = DateTime.UtcNow;

                // If status is active, clear trial end date
                if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    subscription.TrialEndsAt = null;
                }

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Updated subscription {SubscriptionId} for organization {OrganizationUnitId} with status {Status}", 
                    lemonsqueezySubscriptionId, organizationUnitId, status);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {SubscriptionId} for organization {OrganizationUnitId}", 
                    lemonsqueezySubscriptionId, organizationUnitId);
                throw;
            }
        }

        public async Task<Subscription> GetOrCreateTrialSubscriptionAsync(Guid organizationUnitId)
        {
            try
            {
                var existingSubscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                
                if (existingSubscription != null)
                {
                    return existingSubscription;
                }

                return await CreateTrialSubscriptionAsync(organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating trial subscription for organization {OrganizationUnitId}", organizationUnitId);
                throw;
            }
        }

        public async Task<bool> IsInTrialPeriodAsync(Guid organizationUnitId)
        {
            try
            {
                var subscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                
                if (subscription == null)
                    return false;

                // Check if trial has expired
                if (subscription.Status == "trialing" && subscription.TrialEndsAt.HasValue && subscription.TrialEndsAt.Value < DateTime.UtcNow)
                {
                    return false; // Trial has expired
                }

                return subscription.IsInTrial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trial period for organization {OrganizationUnitId}", organizationUnitId);
                return false;
            }
        }

        public async Task<bool> IsOrganizationUnitEligibleForTrialAsync(Guid organizationUnitId, string userId)
        {
            try
            {
                // Parse userId to Guid
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return false;
                }

                // Check if user has already used a trial (ANY past trial, regardless of current status)
                var userTrialSubscriptions = await _unitOfWork.Subscriptions
                    .GetAllIgnoringFiltersAsync(s => s.CreatedBy == userGuid && s.TrialEndsAt != null);
                
                if (userTrialSubscriptions.Any())
                {
                    return false;
                }

                // Check if this is the user's first organization unit
                var userOrganizationUnits = await _unitOfWork.OrganizationUnits
                    .GetAllIgnoringFiltersAsync(ou => ou.CreatedBy == userGuid);
                
                if (!userOrganizationUnits.Any())
                {
                    return false;
                }

                // Find the first organization unit created by this user
                // Handle nullable CreatedAt - units without CreatedAt go to end, then order by actual dates
                var firstOrganizationUnit = userOrganizationUnits
                    .Where(ou => ou.CreatedAt.HasValue) // Only consider units with valid CreatedAt
                    .OrderBy(ou => ou.CreatedAt.Value)
                    .FirstOrDefault();

                // If no units have CreatedAt, fall back to ordering by ID (which is always set)
                if (firstOrganizationUnit == null)
                {
                    firstOrganizationUnit = userOrganizationUnits
                        .OrderBy(ou => ou.Id)
                        .First();
                }

                // Only eligible if this is the first organization unit
                return firstOrganizationUnit.Id == organizationUnitId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trial eligibility for organization {OrganizationUnitId} by user {UserId}", organizationUnitId, userId);
                return false;
            }
        }

        public async Task<SubscriptionStatus> GetSubscriptionStatusAsync(Guid organizationUnitId)
        {
            try
            {
                var subscription = await GetCurrentSubscriptionAsync(organizationUnitId);
                
                if (subscription == null)
                {
                    return new SubscriptionStatus
                    {
                        HasSubscription = false,
                        IsActive = false,
                        IsInTrial = false,
                        Status = "none",
                        PlanName = "",
                    };
                }

                // Check if trial has expired and update status accordingly
                var currentStatus = subscription.Status;
                var isActive = subscription.IsActive;
                var isInTrial = subscription.IsInTrial;

                // If subscription status is "trialing" but trial has expired, treat it as expired
                if (subscription.Status == "trialing" && subscription.TrialEndsAt.HasValue && subscription.TrialEndsAt.Value < DateTime.UtcNow)
                {
                    currentStatus = "expired";
                    isActive = false;
                    isInTrial = false;

                    // Update the subscription status in the database for consistency
                    try
                    {
                        subscription.Status = "expired";
                        await _unitOfWork.CompleteAsync();
                        _logger.LogInformation("Updated expired trial subscription status for organization {OrganizationUnitId}", organizationUnitId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update expired trial status for organization {OrganizationUnitId}", organizationUnitId);
                        // Continue with the corrected status even if database update fails
                    }
                }

                var status = new SubscriptionStatus
                {
                    HasSubscription = true,
                    IsActive = isActive,
                    IsInTrial = isInTrial,
                    Status = currentStatus,
                    PlanName = subscription.PlanName,
                    TrialEndsAt = subscription.TrialEndsAt,
                    RenewsAt = subscription.RenewsAt,
                    EndsAt = subscription.EndsAt
                };

                // Calculate days remaining
                if (isInTrial && subscription.TrialEndsAt.HasValue)
                {
                    var daysRemaining = (subscription.TrialEndsAt.Value - DateTime.UtcNow).Days;
                    status.DaysRemaining = Math.Max(0, daysRemaining);
                }
                else if (currentStatus == "active" && subscription.RenewsAt.HasValue)
                {
                    var daysUntilNextBilling = (subscription.RenewsAt.Value - DateTime.UtcNow).Days;
                    status.DaysRemaining = Math.Max(0, daysUntilNextBilling);
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for organization {OrganizationUnitId}", organizationUnitId);
                
                // Return safe default on error
                return new SubscriptionStatus
                {
                    HasSubscription = false,
                    IsActive = false,
                    IsInTrial = false,
                    Status = "error",
                    PlanName = "",
                };
            }
        }
    }
}