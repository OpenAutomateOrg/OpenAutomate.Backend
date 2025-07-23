using OpenAutomate.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for managing subscription lifecycle and validation
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Gets the current subscription for a tenant
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>The current subscription or null if none exists</returns>
        Task<Subscription?> GetCurrentSubscriptionAsync(Guid organizationUnitId);

        /// <summary>
        /// Checks if a tenant has an active subscription (trial or paid)
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>True if the tenant has an active subscription</returns>
        Task<bool> HasActiveSubscriptionAsync(Guid organizationUnitId);

        /// <summary>
        /// Creates a new trial subscription for a tenant
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <param name="trialDays">Number of days for the trial (default 7)</param>
        /// <returns>The created subscription</returns>
        Task<Subscription> CreateTrialSubscriptionAsync(Guid organizationUnitId, int trialDays = 7);

        /// <summary>
        /// Updates subscription from Lemon Squeezy webhook data
        /// </summary>
        /// <param name="lemonsqueezySubscriptionId">Lemon Squeezy subscription ID</param>
        /// <param name="status">New subscription status</param>
        /// <param name="renewsAt">Next renewal date</param>
        /// <param name="endsAt">Subscription end date</param>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>The updated subscription</returns>
        Task<Subscription> UpdateSubscriptionFromWebhookAsync(
            string lemonsqueezySubscriptionId,
            string status,
            DateTime? renewsAt,
            DateTime? endsAt,
            Guid organizationUnitId);

        /// <summary>
        /// Gets or creates a trial subscription for a tenant
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>The subscription (existing or newly created trial)</returns>
        Task<Subscription> GetOrCreateTrialSubscriptionAsync(Guid organizationUnitId);

        /// <summary>
        /// Checks if a tenant is within their trial period
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>True if the tenant is in a trial period</returns>
        Task<bool> IsInTrialPeriodAsync(Guid organizationUnitId);

        /// <summary>
        /// Gets the subscription status for a tenant
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <returns>Subscription status information</returns>
        Task<SubscriptionStatus> GetSubscriptionStatusAsync(Guid organizationUnitId);
    }

    /// <summary>
    /// DTO for subscription status information
    /// </summary>
    public class SubscriptionStatus
    {
        public bool HasSubscription { get; set; }
        public bool IsActive { get; set; }
        public bool IsInTrial { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? RenewsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int? DaysRemaining { get; set; }
    }
}