using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.IServices;
using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    /// <summary>
    /// Service for interacting with Lemon Squeezy API and processing webhooks
    /// </summary>
    public class LemonsqueezyService : ILemonsqueezyService
    {
        private readonly HttpClient _httpClient;
        private readonly LemonSqueezySettings _settings;
        private readonly AppSettings _appSettings;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<LemonsqueezyService> _logger;

        public LemonsqueezyService(
            HttpClient httpClient,
            IOptions<AppSettings> appSettings,
            ISubscriptionService subscriptionService,
            ILogger<LemonsqueezyService> logger)
        {
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
            _settings = appSettings.Value.LemonSqueezy;
            _subscriptionService = subscriptionService;
            _logger = logger;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.api+json");
            // Note: Content-Type is set per-request only for requests with body content
        }

        public async Task<string> CreateCheckoutUrlAsync(Guid organizationUnitId, string userEmail, string? redirectUrl = null)
        {
            // Define success/cancel URLs once for both API and buy link flows
            var successUrl = redirectUrl ?? $"{_appSettings.FrontendUrl}/subscription/success";
            var cancelUrl = $"{_appSettings.FrontendUrl}/dashboard";

            // First try the JSON:API checkouts endpoint
            try
            {
                var checkoutData = new
                {
                    data = new
                    {
                        type = "checkouts",
                        attributes = new
                        {
                            product_options = new
                            {
                                name = "Pro Subscription",
                                description = "OpenAutomate Pro Plan",
                                media = new string[0],
                                redirect_url = successUrl,
                                receipt_button_text = "Return to app",
                                receipt_link_url = successUrl
                            },
                            checkout_options = new
                            {
                                embed = true,
                                media = true,
                                logo = true,
                                success_url = successUrl,
                                cancel_url = cancelUrl
                            },
                            checkout_data = new
                            {
                                email = userEmail,
                                custom = new
                                {
                                    organization_unit_id = organizationUnitId.ToString()
                                }
                            },
                            expires_at = DateTime.UtcNow.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ssZ")
                        },
                        relationships = new
                        {
                            store = new { data = new { type = "stores", id = _settings.StoreId } },
                            variant = new { data = new { type = "variants", id = _settings.ProVariantId } }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(checkoutData);
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.PostAsync("/checkouts", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var checkoutResponse = JsonDocument.Parse(responseContent);
                    var checkoutUrl = checkoutResponse.RootElement
                        .GetProperty("data")
                        .GetProperty("attributes")
                        .GetProperty("url")
                        .GetString();
                    _logger.LogInformation("Created checkout URL for organization {OrganizationUnitId}", organizationUnitId);
                    return checkoutUrl ?? throw new InvalidOperationException("Checkout URL was null");
                }

                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Checkout API failed. Falling back to buy link. Status: {Status} Error: {Error}", response.StatusCode, err);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Checkout API threw. Falling back to buy link for organization {OrganizationUnitId}", organizationUnitId);
            }

            // Fallback: Prefer explicit BuyLinkUrl when configured (newer hosted checkout link)
            // Example: https://your-store.lemonsqueezy.com/buy/{checkout_link_id}
            string? buyUrl = _settings.BuyLinkUrl;

            if (string.IsNullOrWhiteSpace(buyUrl))
            {
                // If no explicit buy link is configured, fetch the variant's buy_now_url
                buyUrl = await GetVariantBuyUrlAsync(_settings.ProVariantId);
            }

            if (string.IsNullOrWhiteSpace(buyUrl))
            {
                // Ultimate fallback to standard host with variant id
                buyUrl = $"https://checkout.lemonsqueezy.com/buy/{_settings.ProVariantId}";
            }



            // Build query parameters supported by Lemon Squeezy buy links
            var uri = new UriBuilder(buyUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            query["checkout[email]"] = userEmail;
            query["checkout[custom][organization_unit_id]"] = organizationUnitId.ToString();
            query["checkout[success_url]"] = successUrl;
            query["checkout[cancel_url]"] = cancelUrl;
            uri.Query = query.ToString();

            var final = uri.ToString();
            _logger.LogInformation("Using buy link fallback for organization {OrganizationUnitId}", organizationUnitId);
            return final;
        }

        private async Task<string?> GetVariantBuyUrlAsync(string variantId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/variants/{variantId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch variant {VariantId}. Status: {Status}", variantId, response.StatusCode);
                    return null;
                }
                var text = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(text);
                var url = json.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("buy_now_url")
                    .GetString();
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching buy link for variant {VariantId}", variantId);
                return null;
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook signature verification failed: empty payload or signature");
                    return false;
                }

                // Lemon Squeezy uses HMAC-SHA256 for webhook signatures
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

                // The signature comes in the format "sha256=<hash>"
                var expectedSignature = signature.StartsWith("sha256=")
                    ? signature.Substring(7)
                    : signature;

                var isValid = string.Equals(computedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning("Webhook signature verification failed. Expected: {Expected}, Computed: {Computed}",
                        expectedSignature, computedSignature);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        public async Task ProcessSubscriptionCreatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload)
        {
            try
            {
                var data = webhookPayload.Data;
                var attributes = data.Attributes;

                // Extract organization unit ID from custom data (check both attributes and meta)
                var organizationUnitId = ExtractOrganizationUnitId(webhookPayload);
                _logger.LogInformation("Extracted organization unit ID from webhook: {OrganizationUnitId}, AttributesCustomData: {AttributesCustomData}, MetaCustomData: {MetaCustomData}",
                    organizationUnitId, JsonSerializer.Serialize(attributes.CustomData), JsonSerializer.Serialize(webhookPayload.Meta?.CustomData));

                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in subscription_created webhook. CustomData: {CustomData}",
                        JsonSerializer.Serialize(attributes.CustomData));
                    return;
                }

                // Update subscription with Lemon Squeezy data
                await _subscriptionService.UpdateSubscriptionFromWebhookAsync(
                    data.Id,
                    attributes.Status ?? "active",
                    attributes.RenewsAt,
                    attributes.EndsAt,
                    organizationUnitId,
                    attributes?.Urls?.CustomerPortal);

                _logger.LogInformation("Processed subscription_created webhook for subscription {SubscriptionId}, organization {OrganizationUnitId}",
                    data.Id, organizationUnitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription_created webhook");
                throw;
            }
        }

        public async Task ProcessSubscriptionUpdatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload)
        {
            try
            {
                var data = webhookPayload.Data;
                var attributes = data.Attributes;

                // Extract organization unit ID from custom data (check both attributes and meta)
                var organizationUnitId = ExtractOrganizationUnitId(webhookPayload);
                _logger.LogInformation("Extracted organization unit ID from subscription_updated webhook: {OrganizationUnitId}", organizationUnitId);

                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in subscription_updated webhook. AttributesCustomData: {AttributesCustomData}, MetaCustomData: {MetaCustomData}",
                        JsonSerializer.Serialize(attributes.CustomData), JsonSerializer.Serialize(webhookPayload.Meta?.CustomData));
                    return;
                }

                // Update subscription with new status
                await _subscriptionService.UpdateSubscriptionFromWebhookAsync(
                    data.Id,
                    attributes.Status ?? "unknown",
                    attributes.RenewsAt,
                    attributes.EndsAt,
                    organizationUnitId,
                    attributes?.Urls?.CustomerPortal);

                // No second call needed now that we pass portal URL in first call

                _logger.LogInformation("Processed subscription_updated webhook for subscription {SubscriptionId}, organization {OrganizationUnitId}, status {Status}",
                    data.Id, organizationUnitId, attributes.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription_updated webhook");
                throw;
            }
        }

        public async Task ProcessOrderCreatedWebhookAsync(LemonsqueezyWebhookPayload webhookPayload)
        {
            try
            {
                var data = webhookPayload.Data;
                var attributes = data.Attributes;

                // Extract organization unit ID from custom data (check both attributes and meta)
                var organizationUnitId = ExtractOrganizationUnitId(webhookPayload);
                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in order_created webhook");
                    return;
                }

                var orderId = data.Id;

                // Try to fetch receipt URL from vendor
                string? receiptUrl = null;
                try
                {
                    receiptUrl = await GetOrderReceiptUrlAsync(orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to fetch receipt URL for order {OrderId}", orderId);
                }

                // Create payment record
                var payment = new Core.Domain.Entities.Payment
                {
                    OrganizationUnitId = organizationUnitId,
                    LemonsqueezyOrderId = orderId,
                    Amount = attributes.Total ?? 0,
                    Currency = attributes.Currency ?? "USD",
                    Status = attributes.OrderStatus ?? "paid",
                    PaymentDate = attributes.CreatedAt ?? DateTime.UtcNow,
                    CustomerEmail = attributes.CustomerEmail,
                    Description = "Pro Subscription Payment",
                    ReceiptUrl = receiptUrl
                };

                // Persist using subscription unit of work via dedicated service later
                // Emitting event through logs for observability
                _logger.LogInformation(
                    "Payment record created for order {OrderId}, organization {OrganizationUnitId}, amount {Amount} {Currency}",
                    payment.LemonsqueezyOrderId, organizationUnitId, payment.Amount, payment.Currency);

                // NOTE: The database insert happens in PaymentService invoked by Webhook controller to maintain SRP
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order_created webhook");
                throw;
            }
        }

        public async Task<string> GetCustomerPortalUrlAsync(string lemonsqueezySubscriptionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/subscriptions/{lemonsqueezySubscriptionId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get subscription details for customer portal. Status: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to get subscription details: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var subscriptionResponse = JsonDocument.Parse(responseContent);

                var customerPortalUrl = subscriptionResponse.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("urls")
                    .GetProperty("customer_portal")
                    .GetString();

                return customerPortalUrl ?? throw new InvalidOperationException("Customer portal URL was null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer portal URL for subscription {SubscriptionId}", lemonsqueezySubscriptionId);
                throw;
            }
        }

        public async Task<LemonsqueezySubscriptionData?> RefreshSubscriptionDataAsync(string lemonsqueezySubscriptionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/subscriptions/{lemonsqueezySubscriptionId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to refresh subscription data. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var subscriptionResponse = JsonDocument.Parse(responseContent);

                var data = subscriptionResponse.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");

                return new LemonsqueezySubscriptionData
                {
                    Id = data.GetProperty("id").GetString() ?? "",
                    Status = attributes.GetProperty("status").GetString() ?? "",
                    RenewsAt = attributes.TryGetProperty("renews_at", out var renewsAt) && renewsAt.ValueKind != JsonValueKind.Null
                        ? ParseLemonSqueezyDate(renewsAt.GetString()) : null,
                    EndsAt = attributes.TryGetProperty("ends_at", out var endsAt) && endsAt.ValueKind != JsonValueKind.Null
                        ? ParseLemonSqueezyDate(endsAt.GetString()) : null,
                    TrialEndsAt = attributes.TryGetProperty("trial_ends_at", out var trialEndsAt) && trialEndsAt.ValueKind != JsonValueKind.Null
                        ? ParseLemonSqueezyDate(trialEndsAt.GetString()) : null,
                    CustomerEmail = attributes.GetProperty("customer_email").GetString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing subscription data for subscription {SubscriptionId}", lemonsqueezySubscriptionId);
                return null;
            }
        }

        public async Task<string?> GetOrderReceiptUrlAsync(string orderId)
        {
            try
            {
                // Lemon Squeezy orders API returns receipt/invoice URLs in attributes.urls
                var response = await _httpClient.GetAsync($"/orders/{orderId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get order details for order {OrderId}. Status: {StatusCode}", orderId, response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(responseContent);
                var root = json.RootElement.GetProperty("data").GetProperty("attributes");

                string? receiptUrl = null;
                if (root.TryGetProperty("urls", out var urls) && urls.ValueKind == JsonValueKind.Object)
                {
                    if (urls.TryGetProperty("invoice_url", out var invoiceUrlEl) && invoiceUrlEl.ValueKind == JsonValueKind.String)
                    {
                        receiptUrl = invoiceUrlEl.GetString();
                    }
                    else if (urls.TryGetProperty("receipt_url", out var receiptUrlEl) && receiptUrlEl.ValueKind == JsonValueKind.String)
                    {
                        receiptUrl = receiptUrlEl.GetString();
                    }
                }

                return receiptUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receipt URL for order {OrderId}", orderId);
                return null;
            }
        }

        private static Guid ExtractOrganizationUnitId(LemonsqueezyWebhookPayload webhookPayload)
        {
            // First try attributes.CustomData (for order webhooks)
            if (webhookPayload.Data.Attributes?.CustomData?.OrganizationUnitId != null &&
                Guid.TryParse(webhookPayload.Data.Attributes.CustomData.OrganizationUnitId, out var orgIdFromAttributes))
            {
                return orgIdFromAttributes;
            }

            // Then try meta.CustomData (for subscription webhooks)
            if (webhookPayload.Meta?.CustomData?.OrganizationUnitId != null &&
                Guid.TryParse(webhookPayload.Meta.CustomData.OrganizationUnitId, out var orgIdFromMeta))
            {
                return orgIdFromMeta;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Safely parses a date string from Lemon Squeezy API using culture-invariant parsing
        /// </summary>
        /// <param name="dateString">The date string from Lemon Squeezy</param>
        /// <returns>Parsed DateTime or null if parsing fails</returns>
        private static DateTime? ParseLemonSqueezyDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // Try parsing as ISO 8601 format with culture-invariant settings
            if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
            {
                return dateTimeOffset.DateTime;
            }

            // Fallback: try standard DateTime parsing with invariant culture
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                return dateTime;
            }

            // If all parsing attempts fail, return null
            return null;
        }
    }
}