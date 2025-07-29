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
    /// Monthly variant ID for the Premium product
    /// </summary>
    public string VariantId { get; set; } = string.Empty;

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
    /// For testing, set to smaller values like 5 minutes
    /// </summary>
    public int TrialDurationMinutes { get; set; } = 10080; // 7 days default

    /// <summary>
    /// Whether to automatically create trial subscriptions when first Organization Unit is created
    /// Set to false for manual trial activation
    /// </summary>
    public bool EnableAutoTrialCreation { get; set; } = true;
}