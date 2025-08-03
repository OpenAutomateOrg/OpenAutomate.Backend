using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for interacting with Lemon Squeezy API and processing webhooks
    /// </summary>
    public interface ILemonsqueezyService
    {
        /// <summary>
        /// Creates a checkout URL for the Premium subscription
        /// </summary>
        /// <param name="organizationUnitId">The tenant ID</param>
        /// <param name="userEmail">Customer email address</param>
        /// <param name="redirectUrl">URL to redirect after successful payment</param>
        /// <returns>The checkout URL</returns>
        Task<string> CreateCheckoutUrlAsync(Guid organizationUnitId, string userEmail, string? redirectUrl = null);

        /// <summary>
        /// Verifies the webhook signature from Lemon Squeezy
        /// </summary>
        /// <param name="payload">The webhook payload</param>
        /// <param name="signature">The X-Signature header value</param>
        /// <returns>True if the signature is valid</returns>
        bool VerifyWebhookSignature(string payload, string signature);

        /// <summary>
        /// Processes a subscription_created webhook event
        /// </summary>
        /// <param name="webhookPayload">The webhook payload</param>
        /// <returns>Task</returns>
        Task ProcessSubscriptionCreatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload);

        /// <summary>
        /// Processes a subscription_updated webhook event
        /// </summary>
        /// <param name="webhookPayload">The webhook payload</param>
        /// <returns>Task</returns>
        Task ProcessSubscriptionUpdatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload);

        /// <summary>
        /// Processes an order_created webhook event
        /// </summary>
        /// <param name="webhookPayload">The webhook payload</param>
        /// <returns>Task</returns>
        Task ProcessOrderCreatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload);

        /// <summary>
        /// Gets the customer portal URL for managing subscription
        /// </summary>
        /// <param name="lemonsqueezySubscriptionId">The Lemon Squeezy subscription ID</param>
        /// <returns>The customer portal URL</returns>
        Task<string> GetCustomerPortalUrlAsync(string lemonsqueezySubscriptionId);

        /// <summary>
        /// Refreshes subscription data from Lemon Squeezy API
        /// </summary>
        /// <param name="lemonsqueezySubscriptionId">The Lemon Squeezy subscription ID</param>
        /// <returns>Updated subscription data</returns>
        Task<LemonsqueezySubscriptionData?> RefreshSubscriptionDataAsync(string lemonsqueezySubscriptionId);
    }

    /// <summary>
    /// Webhook payload structure from Lemon Squeezy
    /// </summary>
    public class LemonsqueezyWebhookPayload
    {
        public string EventName { get; set; } = string.Empty;
        public LemonsqueezyWebhookData Data { get; set; } = new();
    }

    /// <summary>
    /// Webhook data structure
    /// </summary>
    public class LemonsqueezyWebhookData
    {
        public string Type { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public LemonsqueezyWebhookAttributes Attributes { get; set; } = new();
    }

    /// <summary>
    /// Webhook attributes structure
    /// </summary>
    public class LemonsqueezyWebhookAttributes
    {
        // Subscription attributes
        public string? Status { get; set; }
        public DateTime? RenewsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public string? CustomerEmail { get; set; }
        public LemonsqueezyCustomData? CustomData { get; set; }

        // Order attributes
        public string? OrderId { get; set; }
        public decimal? Total { get; set; }
        public string? Currency { get; set; }
        public string? OrderStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Custom data structure for passing tenant information
    /// </summary>
    public class LemonsqueezyCustomData
    {
        public string? OrganizationUnitId { get; set; }
    }

    /// <summary>
    /// Subscription data from Lemon Squeezy API
    /// </summary>
    public class LemonsqueezySubscriptionData
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? RenewsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
    }
}