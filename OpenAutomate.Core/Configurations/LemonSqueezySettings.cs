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
}