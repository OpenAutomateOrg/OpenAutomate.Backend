namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Lemon Squeezy integration settings
/// </summary>
public class LemonSqueezySettings
{
    /// <summary>
    /// Lemon Squeezy API key for making API calls
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Lemon Squeezy store ID
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Webhook secret for signature verification
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Premium product ID in Lemon Squeezy
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Pro plan variant ID for the Premium product
    /// </summary>
    public string ProVariantId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Direct hosted checkout buy link URL (e.g.
    /// https://your-store.lemonsqueezy.com/buy/{checkout_link_id}).
    /// When provided, the system will prefer this URL for checkouts and
    /// append prefill parameters (email, custom org id, success/cancel URLs).
    /// </summary>
    public string? BuyLinkUrl { get; set; }

    /// <summary>
    /// Base URL for Lemon Squeezy API (default: https://api.lemonsqueezy.com/v1)
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.lemonsqueezy.com/v1";

    /// <summary>
    /// Whether to use sandbox mode for testing
    /// </summary>
    public bool UseSandbox { get; set; } = false;

    /// <summary>
    /// Trial period duration in minutes (default: 7 days = 10080 minutes)
    /// Common values for testing:
    /// - 2 minutes: 2
    /// - 5 minutes: 5
    /// - 30 minutes: 30
    /// - 1 hour: 60
    /// - 1 day: 1440
    /// - 7 days: 10080
    /// </summary>
    public int TrialDurationMinutes { get; set; } = 10080; // 7 days default

    /// <summary>
    /// Whether to automatically create trial subscriptions when first Organization Unit is created
    /// Set to false for manual trial activation
    /// </summary>
    public bool EnableAutoTrialCreation { get; set; } = true;
}