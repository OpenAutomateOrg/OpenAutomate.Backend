using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.Common;
using OpenAutomate.Core.IServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Persists and queries tenant payment records
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILemonsqueezyService _lemonsqueezyService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, ILemonsqueezyService lemonsqueezyService, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _lemonsqueezyService = lemonsqueezyService;
            _logger = logger;
        }

        public async Task<Payment> UpsertAsync(Payment payment)
        {
            // Try to find existing by orderId to ensure idempotency
            var existing = (await _unitOfWork.Payments.GetAllAsync(p =>
                p.OrganizationUnitId == payment.OrganizationUnitId &&
                p.LemonsqueezyOrderId == payment.LemonsqueezyOrderId)).FirstOrDefault();

            if (existing != null)
            {
                _logger.LogInformation(
                    "Updating payment for tenant {TenantId} order {OrderId}.",
                    payment.OrganizationUnitId,
                    payment.LemonsqueezyOrderId);
                existing.Amount = payment.Amount;
                existing.Currency = payment.Currency;
                existing.Status = payment.Status;
                existing.PaymentDate = payment.PaymentDate;
                existing.CustomerEmail = payment.CustomerEmail;
                existing.Description = payment.Description;
                existing.ReceiptUrl = payment.ReceiptUrl ?? existing.ReceiptUrl;
            }
            else
            {
                _logger.LogInformation(
                    "Creating payment for tenant {TenantId} order {OrderId}.",
                    payment.OrganizationUnitId,
                    payment.LemonsqueezyOrderId);
                await _unitOfWork.Payments.AddAsync(payment);
            }

            await _unitOfWork.CompleteAsync();
            return existing ?? payment;
        }

        public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(Guid organizationUnitId, int pageNumber = 1, int pageSize = 25)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 200) pageSize = 200;

            var queryable = (await _unitOfWork.Payments.GetAllAsync(p => p.OrganizationUnitId == organizationUnitId))
                .AsQueryable()
                .OrderByDescending(p => p.PaymentDate);

            var totalCount = queryable.Count();
            var items = queryable
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    OrderId = p.LemonsqueezyOrderId,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    IsRefunded = p.IsRefunded,
                    ReceiptUrl = p.ReceiptUrl
                })
                .ToList();

            return new PagedResult<PaymentDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Payment?> GetByOrderIdAsync(Guid organizationUnitId, string orderId)
        {
            var items = await _unitOfWork.Payments.GetAllAsync(p =>
                p.OrganizationUnitId == organizationUnitId && p.LemonsqueezyOrderId == orderId);
            return items.FirstOrDefault();
        }

        public async Task<string?> GetReceiptUrlAsync(Guid organizationUnitId, string orderId)
        {
            var payment = await GetByOrderIdAsync(organizationUnitId, orderId);
            if (payment != null && !string.IsNullOrWhiteSpace(payment.ReceiptUrl))
            {
                return payment.ReceiptUrl;
            }

            // Fallback to vendor API
            _logger.LogInformation(
                "Fetching receipt URL from vendor API for order {OrderId} (tenant {TenantId}).",
                orderId,
                organizationUnitId);
            var url = await _lemonsqueezyService.GetOrderReceiptUrlAsync(orderId);
            if (payment != null && !string.IsNullOrWhiteSpace(url))
            {
                payment.ReceiptUrl = url;
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation(
                    "Updated stored receipt URL for order {OrderId} (tenant {TenantId}).",
                    orderId,
                    organizationUnitId);
            }
            return url;
        }
    }
}


