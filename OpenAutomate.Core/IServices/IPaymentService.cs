using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for persisting and querying payment records for tenants
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Creates or updates a payment record keyed by vendor order ID.
        /// </summary>
        Task<Payment> UpsertAsync(Payment payment);

        /// <summary>
        /// Returns paginated payments for a tenant ordered by newest first.
        /// </summary>
        Task<PagedResult<PaymentDto>> GetPaymentsAsync(Guid organizationUnitId, int pageNumber = 1, int pageSize = 25);

        /// <summary>
        /// Returns a payment by vendor order ID and tenant.
        /// </summary>
        Task<Payment?> GetByOrderIdAsync(Guid organizationUnitId, string orderId);

        /// <summary>
        /// Returns a vendor-hosted receipt URL for a given order. Will fallback to vendor API when not stored.
        /// </summary>
        Task<string?> GetReceiptUrlAsync(Guid organizationUnitId, string orderId);
    }

    /// <summary>
    /// Lightweight DTO for listing payments to end users
    /// </summary>
    public class PaymentDto
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public bool IsRefunded { get; set; }
        public string? ReceiptUrl { get; set; }
    }
}


