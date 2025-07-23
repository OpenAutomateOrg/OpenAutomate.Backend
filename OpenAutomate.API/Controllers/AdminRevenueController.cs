using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for admin revenue reporting and analytics
    /// </summary>
    [ApiController]
    [Route("api/admin/revenue")]
    [Authorize]
    public class AdminRevenueController : ControllerBase
    {
        private readonly IAdminRevenueService _adminRevenueService;
        private readonly ILogger<AdminRevenueController> _logger;

        public AdminRevenueController(
            IAdminRevenueService adminRevenueService,
            ILogger<AdminRevenueController> logger)
        {
            _adminRevenueService = adminRevenueService;
            _logger = logger;
        }

        /// <summary>
        /// Gets comprehensive revenue metrics and analytics
        /// </summary>
        /// <returns>Revenue metrics including total revenue, MRR, subscription counts, etc.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(RevenueMetricsResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRevenueMetrics()
        {
            try
            {
                // Check if user has admin role
                if (!IsUserAdmin())
                {
                    _logger.LogWarning("Non-admin user attempted to access revenue metrics: {UserId}", GetCurrentUserId());
                    return Forbid("Access denied. Admin role required.");
                }

                _logger.LogInformation("Admin user {UserId} requesting revenue metrics", GetCurrentUserId());

                var metrics = await _adminRevenueService.GetRevenueMetricsAsync();

                var response = new RevenueMetricsResponse
                {
                    TotalRevenue = metrics.TotalRevenue,
                    MonthlyRecurringRevenue = metrics.MonthlyRecurringRevenue,
                    CurrentMonthRevenue = metrics.CurrentMonthRevenue,
                    PreviousMonthRevenue = metrics.PreviousMonthRevenue,
                    RevenueGrowthPercentage = metrics.RevenueGrowthPercentage,
                    ActiveSubscriptions = metrics.ActiveSubscriptions,
                    TrialSubscriptions = metrics.TrialSubscriptions,
                    TotalPayments = metrics.TotalPayments,
                    AverageRevenuePerUser = metrics.AverageRevenuePerUser,
                    TotalSubscribedOrganizations = metrics.TotalSubscribedOrganizations,
                    LastUpdated = metrics.LastUpdated
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue metrics");
                return StatusCode(500, new { message = "An error occurred while retrieving revenue metrics" });
            }
        }

        /// <summary>
        /// Gets monthly revenue breakdown for a specific time period
        /// </summary>
        /// <param name="months">Number of months to include (default: 12)</param>
        /// <returns>Monthly revenue breakdown</returns>
        [HttpGet("monthly")]
        [ProducesResponseType(typeof(MonthlyRevenueResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int months = 12)
        {
            try
            {
                if (!IsUserAdmin())
                {
                    return Forbid("Access denied. Admin role required.");
                }

                if (months < 1 || months > 24)
                {
                    return BadRequest(new { message = "Months parameter must be between 1 and 24" });
                }

                _logger.LogInformation("Admin user {UserId} requesting monthly revenue for {Months} months", 
                    GetCurrentUserId(), months);

                var monthlyRevenue = await _adminRevenueService.GetMonthlyRevenueAsync(months);

                var response = new MonthlyRevenueResponse
                {
                    MonthlyData = monthlyRevenue.MonthlyData.Select(m => new MonthlyRevenueData
                    {
                        Year = m.Year,
                        Month = m.Month,
                        MonthName = m.MonthName,
                        Revenue = m.Revenue,
                        PaymentCount = m.PaymentCount,
                        NewSubscriptions = m.NewSubscriptions
                    }).ToArray(),
                    TotalRevenue = monthlyRevenue.TotalRevenue,
                    AverageMonthlyRevenue = monthlyRevenue.AverageMonthlyRevenue
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly revenue data");
                return StatusCode(500, new { message = "An error occurred while retrieving monthly revenue data" });
            }
        }

        /// <summary>
        /// Gets subscription analytics and insights
        /// </summary>
        /// <returns>Subscription analytics including active trials, conversions, churn, etc.</returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(SubscriptionAnalyticsResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSubscriptionAnalytics()
        {
            try
            {
                if (!IsUserAdmin())
                {
                    return Forbid("Access denied. Admin role required.");
                }

                _logger.LogInformation("Admin user {UserId} requesting subscription analytics", GetCurrentUserId());

                var analytics = await _adminRevenueService.GetSubscriptionAnalyticsAsync();

                var response = new SubscriptionAnalyticsResponse
                {
                    ActiveSubscriptions = analytics.ActiveSubscriptions,
                    TrialSubscriptions = analytics.TrialSubscriptions,
                    ExpiredSubscriptions = analytics.ExpiredSubscriptions,
                    CancelledSubscriptions = analytics.CancelledSubscriptions,
                    TrialConversionRate = analytics.TrialConversionRate,
                    MonthlyChurnRate = analytics.MonthlyChurnRate,
                    TrialsExpiringThisWeek = analytics.TrialsExpiringThisWeek,
                    SubscriptionsRenewingThisWeek = analytics.SubscriptionsRenewingThisWeek,
                    AverageSubscriptionLifetime = analytics.AverageSubscriptionLifetime
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription analytics");
                return StatusCode(500, new { message = "An error occurred while retrieving subscription analytics" });
            }
        }

        /// <summary>
        /// Checks if the current user has admin role
        /// </summary>
        private bool IsUserAdmin()
        {
            var roleClaimValue = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (Enum.TryParse<SystemRole>(roleClaimValue, ignoreCase: true, out var role))
            {
                return role == SystemRole.Admin;
            }

            return false;
        }

        /// <summary>
        /// Gets the current user's ID from JWT claims
        /// </summary>
        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
        }
    }

    /// <summary>
    /// Response model for comprehensive revenue metrics
    /// </summary>
    public class RevenueMetricsResponse
    {
        /// <summary>
        /// Total revenue from all payments
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Monthly Recurring Revenue (based on active subscriptions)
        /// </summary>
        public decimal MonthlyRecurringRevenue { get; set; }

        /// <summary>
        /// Revenue for the current month
        /// </summary>
        public decimal CurrentMonthRevenue { get; set; }

        /// <summary>
        /// Revenue for the previous month
        /// </summary>
        public decimal PreviousMonthRevenue { get; set; }

        /// <summary>
        /// Month-over-month revenue growth percentage
        /// </summary>
        public decimal RevenueGrowthPercentage { get; set; }

        /// <summary>
        /// Total number of active subscriptions
        /// </summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>
        /// Total number of trial subscriptions
        /// </summary>
        public int TrialSubscriptions { get; set; }

        /// <summary>
        /// Total number of payments processed
        /// </summary>
        public int TotalPayments { get; set; }

        /// <summary>
        /// Average revenue per user
        /// </summary>
        public decimal AverageRevenuePerUser { get; set; }

        /// <summary>
        /// Total number of organization units with subscriptions
        /// </summary>
        public int TotalSubscribedOrganizations { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Response model for monthly revenue breakdown
    /// </summary>
    public class MonthlyRevenueResponse
    {
        /// <summary>
        /// Monthly revenue data points
        /// </summary>
        public MonthlyRevenueData[] MonthlyData { get; set; } = Array.Empty<MonthlyRevenueData>();

        /// <summary>
        /// Total revenue across all months
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Average monthly revenue
        /// </summary>
        public decimal AverageMonthlyRevenue { get; set; }
    }

    /// <summary>
    /// Monthly revenue data point
    /// </summary>
    public class MonthlyRevenueData
    {
        /// <summary>
        /// Year of the data point
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Month of the data point (1-12)
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Month name (e.g., "January")
        /// </summary>
        public string MonthName { get; set; } = string.Empty;

        /// <summary>
        /// Total revenue for the month
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Number of payments in the month
        /// </summary>
        public int PaymentCount { get; set; }

        /// <summary>
        /// Number of new subscriptions started in the month
        /// </summary>
        public int NewSubscriptions { get; set; }
    }

    /// <summary>
    /// Response model for subscription analytics
    /// </summary>
    public class SubscriptionAnalyticsResponse
    {
        /// <summary>
        /// Total active subscriptions
        /// </summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>
        /// Total trial subscriptions
        /// </summary>
        public int TrialSubscriptions { get; set; }

        /// <summary>
        /// Total expired subscriptions
        /// </summary>
        public int ExpiredSubscriptions { get; set; }

        /// <summary>
        /// Total cancelled subscriptions
        /// </summary>
        public int CancelledSubscriptions { get; set; }

        /// <summary>
        /// Trial to paid conversion rate (percentage)
        /// </summary>
        public decimal TrialConversionRate { get; set; }

        /// <summary>
        /// Monthly churn rate (percentage)
        /// </summary>
        public decimal MonthlyChurnRate { get; set; }

        /// <summary>
        /// Trials expiring in the next 7 days
        /// </summary>
        public int TrialsExpiringThisWeek { get; set; }

        /// <summary>
        /// Subscriptions renewing in the next 7 days
        /// </summary>
        public int SubscriptionsRenewingThisWeek { get; set; }

        /// <summary>
        /// Average subscription lifetime in days
        /// </summary>
        public decimal AverageSubscriptionLifetime { get; set; }
    }
}