using System;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    /// <summary>
    /// Represents a payment transaction for a tenant/organization unit
    /// </summary>
    public class Payment : TenantEntity
    {
        /// <summary>
        /// The order ID from Lemon Squeezy
        /// </summary>
        public string LemonsqueezyOrderId { get; set; } = string.Empty;

        /// <summary>
        /// The subscription ID this payment is for (if applicable)
        /// </summary>
        public string? LemonsqueezySubscriptionId { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., "USD")
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Payment status (paid, refunded, failed, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the payment was processed
        /// </summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Description of what the payment is for
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Customer email from the payment
        /// </summary>
        public string? CustomerEmail { get; set; }

        /// <summary>
        /// Whether this payment was successful
        /// </summary>
        public bool IsSuccessful => Status.Equals("paid", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Whether this payment was refunded
        /// </summary>
        public bool IsRefunded => Status.Equals("refunded", StringComparison.OrdinalIgnoreCase);
    }
}