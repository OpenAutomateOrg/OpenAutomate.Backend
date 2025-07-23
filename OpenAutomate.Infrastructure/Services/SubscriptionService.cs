using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            ILogger<SubscriptionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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

                return subscription.IsInTrial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trial period for organization {OrganizationUnitId}", organizationUnitId);
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

                var status = new SubscriptionStatus
                {
                    HasSubscription = true,
                    IsActive = subscription.IsActive,
                    IsInTrial = subscription.IsInTrial,
                    Status = subscription.Status,
                    PlanName = subscription.PlanName,
                    TrialEndsAt = subscription.TrialEndsAt,
                    RenewsAt = subscription.RenewsAt,
                    EndsAt = subscription.EndsAt
                };

                // Calculate days remaining
                if (subscription.IsInTrial && subscription.TrialEndsAt.HasValue)
                {
                    var daysRemaining = (subscription.TrialEndsAt.Value - DateTime.UtcNow).Days;
                    status.DaysRemaining = Math.Max(0, daysRemaining);
                }
                else if (subscription.Status == "active" && subscription.RenewsAt.HasValue)
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