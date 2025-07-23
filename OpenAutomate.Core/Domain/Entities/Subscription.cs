using System;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a subscription for a tenant/organization unit
    /// </summary>
    public class Subscription : TenantEntity
    {
        /// <summary>
        /// The subscription ID from Lemon Squeezy
        /// </summary>
        public string? LemonsqueezySubscriptionId { get; set; }

        /// <summary>
        /// The subscription plan name (e.g., "Premium")
        /// </summary>
        public string PlanName { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the subscription (trialing, active, expired, cancelled)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the trial period ends (if in trial)
        /// </summary>
        public DateTime? TrialEndsAt { get; set; }

        /// <summary>
        /// When the subscription renews next
        /// </summary>
        public DateTime? RenewsAt { get; set; }

        /// <summary>
        /// When the subscription fully expires
        /// </summary>
        public DateTime? EndsAt { get; set; }

        /// <summary>
        /// Whether the subscription is currently active (trial or paid)
        /// </summary>
        public bool IsActive => Status == "trialing" || Status == "active";

        /// <summary>
        /// Whether the subscription is in trial period
        /// </summary>
        public bool IsInTrial => Status == "trialing" && TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
    }
}