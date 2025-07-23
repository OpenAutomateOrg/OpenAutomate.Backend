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
                                name = "Premium Subscription",
                                description = "OpenAutomate Premium Plan with unlimited AI messages",
                                media = new string[0],
                                redirect_url = redirectUrl ?? $"{_appSettings.FrontendUrl}/subscription/success",
                                receipt_button_text = "Go to Dashboard",
                                receipt_link_url = redirectUrl ?? $"{_appSettings.FrontendUrl}/dashboard"
                            },
                            checkout_options = new
                            {
                                embed = false,
                                media = true,
                                logo = true
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
                            store = new
                            {
                                data = new
                                {
                                    type = "stores",
                                    id = _settings.StoreId
                                }
                            },
                            variant = new
                            {
                                data = new
                                {
                                    type = "variants",
                                    id = _settings.VariantId
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(checkoutData);
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.PostAsync("/checkouts", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create checkout URL. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to create checkout URL: {response.StatusCode}");
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout URL for organization {OrganizationUnitId}", organizationUnitId);
                throw;
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

                // Extract organization unit ID from custom data
                var organizationUnitId = ExtractOrganizationUnitId(attributes.CustomData);
                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in subscription_created webhook");
                    return;
                }

                // Update subscription with Lemon Squeezy data
                await _subscriptionService.UpdateSubscriptionFromWebhookAsync(
                    data.Id,
                    attributes.Status ?? "active",
                    attributes.RenewsAt,
                    attributes.EndsAt,
                    organizationUnitId);

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

                // Extract organization unit ID from custom data
                var organizationUnitId = ExtractOrganizationUnitId(attributes.CustomData);
                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in subscription_updated webhook");
                    return;
                }

                // Update subscription with new status
                await _subscriptionService.UpdateSubscriptionFromWebhookAsync(
                    data.Id,
                    attributes.Status ?? "unknown",
                    attributes.RenewsAt,
                    attributes.EndsAt,
                    organizationUnitId);

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

                // Extract organization unit ID from custom data
                var organizationUnitId = ExtractOrganizationUnitId(attributes.CustomData);
                if (organizationUnitId == Guid.Empty)
                {
                    _logger.LogError("No valid organization unit ID found in order_created webhook");
                    return;
                }

                // Create payment record
                var payment = new Core.Domain.Entities.Payment
                {
                    OrganizationUnitId = organizationUnitId,
                    LemonsqueezyOrderId = attributes.OrderId ?? data.Id,
                    Amount = attributes.Total ?? 0,
                    Currency = attributes.Currency ?? "USD",
                    Status = attributes.OrderStatus ?? "paid",
                    PaymentDate = attributes.CreatedAt ?? DateTime.UtcNow,
                    CustomerEmail = attributes.CustomerEmail,
                    Description = "Premium Subscription Payment"
                };

                // This would typically use a payment repository or service
                // For now, we'll log the payment creation
                _logger.LogInformation("Payment record created for order {OrderId}, organization {OrganizationUnitId}, amount {Amount} {Currency}", 
                    payment.LemonsqueezyOrderId, organizationUnitId, payment.Amount, payment.Currency);

                // TODO: Add payment to database through a payment service
                // await _paymentService.CreatePaymentAsync(payment);
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

        private static Guid ExtractOrganizationUnitId(LemonsqueezyCustomData? customData)
        {
            if (customData?.OrganizationUnitId != null && 
                Guid.TryParse(customData.OrganizationUnitId, out var organizationUnitId))
            {
                return organizationUnitId;
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