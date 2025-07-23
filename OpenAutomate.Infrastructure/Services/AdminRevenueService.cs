using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for admin revenue reporting and analytics
    /// </summary>
    public class AdminRevenueService : IAdminRevenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminRevenueService> _logger;

        public AdminRevenueService(IUnitOfWork unitOfWork, ILogger<AdminRevenueService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RevenueMetrics> GetRevenueMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Calculating revenue metrics");

                // Get all payments (ignoring tenant filters for admin view)
                var allPayments = await _unitOfWork.Payments.GetAllIgnoringFiltersAsync(p => p.Status == "paid");
                var payments = allPayments.ToList();

                // Get all subscriptions (ignoring tenant filters for admin view)
                var allSubscriptions = await _unitOfWork.Subscriptions.GetAllIgnoringFiltersAsync();
                var subscriptions = allSubscriptions.ToList();

                // Calculate total revenue
                var totalRevenue = payments.Sum(p => p.Amount);

                // Calculate current and previous month revenue
                var now = DateTime.UtcNow;
                var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                var previousMonthStart = currentMonthStart.AddMonths(-1);

                var currentMonthRevenue = payments
                    .Where(p => p.PaymentDate >= currentMonthStart)
                    .Sum(p => p.Amount);

                var previousMonthRevenue = payments
                    .Where(p => p.PaymentDate >= previousMonthStart && p.PaymentDate < currentMonthStart)
                    .Sum(p => p.Amount);

                // Calculate revenue growth percentage
                var revenueGrowthPercentage = previousMonthRevenue > 0 
                    ? ((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue) * 100 
                    : 0;

                // Count subscription statuses - use consistent logic across all methods
                var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
                var trialSubscriptions = subscriptions.Count(s => s.Status == "trialing");

                // Calculate MRR (Monthly Recurring Revenue)
                // Use actual monthly revenue from recent payments rather than incorrect historical average
                var monthlyRecurringRevenue = CalculateMonthlyRecurringRevenue(payments, activeSubscriptions);

                // Calculate ARPU (Average Revenue Per User)
                var uniqueOrganizations = subscriptions.Select(s => s.OrganizationUnitId).Distinct().Count();
                var averageRevenuePerUser = uniqueOrganizations > 0 ? totalRevenue / uniqueOrganizations : 0;

                var metrics = new RevenueMetrics
                {
                    TotalRevenue = totalRevenue,
                    MonthlyRecurringRevenue = monthlyRecurringRevenue,
                    CurrentMonthRevenue = currentMonthRevenue,
                    PreviousMonthRevenue = previousMonthRevenue,
                    RevenueGrowthPercentage = revenueGrowthPercentage,
                    ActiveSubscriptions = activeSubscriptions,
                    TrialSubscriptions = trialSubscriptions,
                    TotalPayments = payments.Count,
                    AverageRevenuePerUser = averageRevenuePerUser,
                    TotalSubscribedOrganizations = uniqueOrganizations,
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Revenue metrics calculated successfully. Total Revenue: {TotalRevenue}, MRR: {MRR}", 
                    totalRevenue, monthlyRecurringRevenue);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating revenue metrics");
                throw;
            }
        }

        public async Task<MonthlyRevenue> GetMonthlyRevenueAsync(int months)
        {
            try
            {
                _logger.LogInformation("Calculating monthly revenue for {Months} months", months);

                // Get all payments (ignoring tenant filters for admin view)
                var allPayments = await _unitOfWork.Payments.GetAllIgnoringFiltersAsync(p => p.Status == "paid");
                var payments = allPayments.ToList();

                // Get all subscriptions for new subscription counts
                var allSubscriptions = await _unitOfWork.Subscriptions.GetAllIgnoringFiltersAsync();
                var subscriptions = allSubscriptions.ToList();

                var monthlyData = new List<MonthlyRevenueDataPoint>();
                var now = DateTime.UtcNow;

                for (int i = months - 1; i >= 0; i--)
                {
                    var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);

                    var monthlyPayments = payments
                        .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < monthEnd)
                        .ToList();

                    var monthlyNewSubscriptions = subscriptions
                        .Where(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd)
                        .Count();

                    monthlyData.Add(new MonthlyRevenueDataPoint
                    {
                        Year = monthStart.Year,
                        Month = monthStart.Month,
                        MonthName = monthStart.ToString("MMMM", CultureInfo.InvariantCulture),
                        Revenue = monthlyPayments.Sum(p => p.Amount),
                        PaymentCount = monthlyPayments.Count,
                        NewSubscriptions = monthlyNewSubscriptions
                    });
                }

                var totalRevenue = monthlyData.Sum(m => m.Revenue);
                var averageMonthlyRevenue = monthlyData.Count > 0 ? totalRevenue / monthlyData.Count : 0;

                var result = new MonthlyRevenue
                {
                    MonthlyData = monthlyData.ToArray(),
                    TotalRevenue = totalRevenue,
                    AverageMonthlyRevenue = averageMonthlyRevenue
                };

                _logger.LogInformation("Monthly revenue calculated for {Months} months. Total: {TotalRevenue}", 
                    months, totalRevenue);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly revenue");
                throw;
            }
        }

        public async Task<SubscriptionAnalytics> GetSubscriptionAnalyticsAsync()
        {
            try
            {
                _logger.LogInformation("Calculating subscription analytics");

                // Get all subscriptions (ignoring tenant filters for admin view)
                var allSubscriptions = await _unitOfWork.Subscriptions.GetAllIgnoringFiltersAsync();
                var subscriptions = allSubscriptions.ToList();

                var now = DateTime.UtcNow;
                var oneWeekFromNow = now.AddDays(7);

                // Count subscriptions by status
                var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
                var trialSubscriptions = subscriptions.Count(s => s.Status == "trialing");
                var expiredSubscriptions = subscriptions.Count(s => s.Status == "expired");
                var cancelledSubscriptions = subscriptions.Count(s => s.Status == "cancelled");

                // Calculate trial conversion rate
                var totalTrials = subscriptions.Count(s => s.TrialEndsAt.HasValue);
                var convertedTrials = subscriptions.Count(s => s.TrialEndsAt.HasValue && s.Status == "active");
                var trialConversionRate = totalTrials > 0 ? (decimal)convertedTrials / totalTrials * 100 : 0;

                // Calculate monthly churn rate (approximation)
                var thirtyDaysAgo = now.AddDays(-30);
                var cancelledLastMonth = subscriptions.Count(s => 
                    s.Status == "cancelled" && s.LastModifyAt >= thirtyDaysAgo);
                // Count subscriptions that were active at the start of the month (exclude those cancelled before the period)
                var activeAtStartOfMonth = subscriptions.Count(s => 
                    s.CreatedAt < thirtyDaysAgo && 
                    (s.Status == "active" || (s.Status == "cancelled" && s.LastModifyAt >= thirtyDaysAgo)));
                var monthlyChurnRate = activeAtStartOfMonth > 0 ? (decimal)cancelledLastMonth / activeAtStartOfMonth * 100 : 0;

                // Count upcoming events
                var trialsExpiringThisWeek = subscriptions.Count(s => 
                    s.Status == "trialing" && s.TrialEndsAt.HasValue && 
                    s.TrialEndsAt.Value >= now && s.TrialEndsAt.Value <= oneWeekFromNow);

                var subscriptionsRenewingThisWeek = subscriptions.Count(s => 
                    s.Status == "active" && s.RenewsAt.HasValue && 
                    s.RenewsAt.Value >= now && s.RenewsAt.Value <= oneWeekFromNow);

                // Calculate average subscription lifetime
                var endedSubscriptions = subscriptions.Where(s => 
                    (s.Status == "cancelled" || s.Status == "expired") && s.CreatedAt.HasValue).ToList();
                var averageLifetime = endedSubscriptions.Count > 0 
                    ? endedSubscriptions.Average(s => 
                        (s.EndsAt ?? s.LastModifyAt ?? s.CreatedAt!.Value.AddDays(1)).Subtract(s.CreatedAt!.Value).TotalDays)
                    : 0;

                var analytics = new SubscriptionAnalytics
                {
                    ActiveSubscriptions = activeSubscriptions,
                    TrialSubscriptions = trialSubscriptions,
                    ExpiredSubscriptions = expiredSubscriptions,
                    CancelledSubscriptions = cancelledSubscriptions,
                    TrialConversionRate = trialConversionRate,
                    MonthlyChurnRate = monthlyChurnRate,
                    TrialsExpiringThisWeek = trialsExpiringThisWeek,
                    SubscriptionsRenewingThisWeek = subscriptionsRenewingThisWeek,
                    AverageSubscriptionLifetime = (decimal)averageLifetime
                };

                _logger.LogInformation("Subscription analytics calculated. Active: {Active}, Trials: {Trials}, Conversion Rate: {ConversionRate}%", 
                    activeSubscriptions, trialSubscriptions, trialConversionRate);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating subscription analytics");
                throw;
            }
        }

        /// <summary>
        /// Calculates Monthly Recurring Revenue based on recent payment patterns
        /// </summary>
        private decimal CalculateMonthlyRecurringRevenue(
            IList<OpenAutomate.Core.Domain.Entities.Payment> payments,
            int activeSubscriptions)
        {
            if (!payments.Any() || activeSubscriptions == 0)
                return 0;

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            
            // Get payments from the last 30 days to estimate monthly revenue
            var recentPayments = payments.Where(p => p.PaymentDate >= thirtyDaysAgo).ToList();
            
            if (!recentPayments.Any())
            {
                // Fallback: If no recent payments, estimate based on active subscriptions
                // TODO: Replace with actual subscription plan pricing from configuration
                // This assumes a default monthly subscription value - should be configurable
                var estimatedMonthlyPrice = 299000m; // 299,000 VND placeholder - should be from config
                return activeSubscriptions * estimatedMonthlyPrice;
            }
            
            // Calculate average monthly revenue from recent payment patterns
            var recentMonthlyRevenue = recentPayments.Sum(p => p.Amount);
            
            // If we have less than 30 days of data, extrapolate
            var daysOfData = (now - recentPayments.Min(p => p.PaymentDate)).TotalDays;
            if (daysOfData < 30 && daysOfData > 0)
            {
                recentMonthlyRevenue = recentMonthlyRevenue * (30m / (decimal)daysOfData);
            }
            
            return recentMonthlyRevenue;
        }
    }
}