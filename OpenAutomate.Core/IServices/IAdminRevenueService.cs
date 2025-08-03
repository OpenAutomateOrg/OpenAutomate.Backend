using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for admin revenue reporting and analytics
    /// </summary>
    public interface IAdminRevenueService
    {
        /// <summary>
        /// Gets comprehensive revenue metrics and analytics
        /// </summary>
        /// <returns>Revenue metrics including total revenue, MRR, subscription counts, etc.</returns>
        Task<RevenueMetrics> GetRevenueMetricsAsync();

        /// <summary>
        /// Gets monthly revenue breakdown for a specific time period
        /// </summary>
        /// <param name="months">Number of months to include</param>
        /// <returns>Monthly revenue breakdown</returns>
        Task<MonthlyRevenue> GetMonthlyRevenueAsync(int months);

        /// <summary>
        /// Gets subscription analytics and insights
        /// </summary>
        /// <returns>Subscription analytics including active trials, conversions, churn, etc.</returns>
        Task<SubscriptionAnalytics> GetSubscriptionAnalyticsAsync();
    }

    /// <summary>
    /// Revenue metrics data model
    /// </summary>
    public class RevenueMetrics
    {
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
        public decimal PreviousMonthRevenue { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int TotalPayments { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
        public int TotalSubscribedOrganizations { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Monthly revenue data model
    /// </summary>
    public class MonthlyRevenue
    {
        public MonthlyRevenueDataPoint[] MonthlyData { get; set; } = Array.Empty<MonthlyRevenueDataPoint>();
        public decimal TotalRevenue { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
    }

    /// <summary>
    /// Monthly revenue data point
    /// </summary>
    public class MonthlyRevenueDataPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int PaymentCount { get; set; }
        public int NewSubscriptions { get; set; }
    }

    /// <summary>
    /// Subscription analytics data model
    /// </summary>
    public class SubscriptionAnalytics
    {
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int CancelledSubscriptions { get; set; }
        public decimal TrialConversionRate { get; set; }
        public decimal MonthlyChurnRate { get; set; }
        public int TrialsExpiringThisWeek { get; set; }
        public int SubscriptionsRenewingThisWeek { get; set; }
        public decimal AverageSubscriptionLifetime { get; set; }
    }
}