using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAutomate.Core.IServices;
using System;
using System.Text;
using System.Text.Json;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for receiving and processing Lemon Squeezy webhooks
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Webhooks don't use regular authentication
    public class LemonsqueezyWebhookController : ControllerBase
    {
        private readonly ILemonsqueezyService _lemonsqueezyService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<LemonsqueezyWebhookController> _logger;

        public LemonsqueezyWebhookController(
            ILemonsqueezyService lemonsqueezyService,
            IPaymentService paymentService,
            ILogger<LemonsqueezyWebhookController> logger)
        {
            _lemonsqueezyService = lemonsqueezyService;
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Receives webhook events from Lemon Squeezy
        /// </summary>
        /// <returns>200 OK if processed successfully, 401 if signature invalid, 400 if processing failed</returns>
        [HttpPost("webhook")]
        public async Task<IActionResult> ProcessWebhook()
        {
            try
            {
                // Read the raw request body
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var payload = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(payload))
                {
                    _logger.LogWarning("Received empty webhook payload");
                    return BadRequest("Empty payload");
                }

                // Get the signature from headers (Lemon Squeezy uses X-Signature)
                var signature = Request.Headers["X-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook received without signature header");
                    return BadRequest("Missing signature header");
                }

                // Verify webhook signature
                if (!_lemonsqueezyService.VerifyWebhookSignature(payload, signature))
                {
                    _logger.LogWarning("Webhook signature verification failed");
                    return Unauthorized("Invalid signature");
                }

                // Parse the webhook payload
                LemonsqueezyWebhookPayload? webhookPayload;
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true
                    };
                    
                    webhookPayload = JsonSerializer.Deserialize<LemonsqueezyWebhookPayload>(payload, options);
                    
                    // Extract event name from meta if not set in root
                    if (string.IsNullOrEmpty(webhookPayload?.EventName) && webhookPayload?.Meta != null)
                    {
                        webhookPayload.EventName = webhookPayload.Meta.EventName;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse webhook payload: {Payload}", payload);
                    return BadRequest("Invalid JSON payload");
                }

                if (webhookPayload == null)
                {
                    _logger.LogWarning("Webhook payload deserialized to null");
                    return BadRequest("Invalid payload structure");
                }

                // Process webhook based on event type
                await ProcessWebhookEvent(webhookPayload);

                _logger.LogInformation("Successfully processed webhook event: {EventName}", webhookPayload.EventName);
                return Ok(new { message = "Webhook processed successfully", eventType = webhookPayload.EventName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task ProcessWebhookEvent(LemonsqueezyWebhookPayload webhookPayload)
        {
            _logger.LogInformation("Processing webhook event: {EventName} for resource {ResourceType} with ID {ResourceId}", 
                webhookPayload.EventName, webhookPayload.Data.Type, webhookPayload.Data.Id);
            
            // Enhanced logging for debugging subscription activation
            _logger.LogInformation("Webhook payload details: CustomData={CustomData}, Status={Status}", 
                System.Text.Json.JsonSerializer.Serialize(webhookPayload.Data.Attributes?.CustomData),
                webhookPayload.Data.Attributes?.Status);

            switch (webhookPayload.EventName.ToLowerInvariant())
            {
                case "subscription_created":
                    await _lemonsqueezyService.ProcessSubscriptionCreatedWebhookAsync(webhookPayload);
                    break;

                case "subscription_updated":
                    await _lemonsqueezyService.ProcessSubscriptionUpdatedWebhookAsync(webhookPayload);
                    break;

                case "order_created":
                    await HandleOrderWebhook(webhookPayload, null);
                    break;

                case "order_refunded":
                    await HandleOrderWebhook(webhookPayload, "refunded");
                    break;

                case "subscription_cancelled":
                    // Handle as subscription_updated with cancelled status
                    await _lemonsqueezyService.ProcessSubscriptionUpdatedWebhookAsync(webhookPayload);
                    break;

                case "subscription_resumed":
                    // Handle as subscription_updated with active status
                    await _lemonsqueezyService.ProcessSubscriptionUpdatedWebhookAsync(webhookPayload);
                    break;

                case "subscription_expired":
                    // Handle as subscription_updated with expired status
                    await _lemonsqueezyService.ProcessSubscriptionUpdatedWebhookAsync(webhookPayload);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventName}", webhookPayload.EventName);
                    break;
            }
        }

        private async Task HandleOrderWebhook(LemonsqueezyWebhookPayload webhookPayload, string? overrideStatus)
        {
            var data = webhookPayload.Data;
            var attributes = data.Attributes;

            // Extract tenant id from attributes.custom_data or meta.custom_data
            Guid organizationUnitId;
            if (attributes?.CustomData?.OrganizationUnitId != null && Guid.TryParse(attributes.CustomData.OrganizationUnitId, out var orgIdFromAttributes))
            {
                organizationUnitId = orgIdFromAttributes;
            }
            else if (webhookPayload.Meta?.CustomData?.OrganizationUnitId != null && Guid.TryParse(webhookPayload.Meta.CustomData.OrganizationUnitId, out var orgIdFromMeta))
            {
                organizationUnitId = orgIdFromMeta;
            }
            else
            {
                _logger.LogWarning("Order webhook missing or invalid organization unit id (checked both attributes and meta)");
                return;
            }

            var orderId = data.Id;
            string? receiptUrl = await _lemonsqueezyService.GetOrderReceiptUrlAsync(orderId);

            var payment = new OpenAutomate.Core.Domain.Entities.Payment
            {
                OrganizationUnitId = organizationUnitId,
                LemonsqueezyOrderId = orderId,
                LemonsqueezySubscriptionId = null,
                Amount = attributes.Total ?? 0,
                Currency = attributes.Currency ?? "USD",
                Status = overrideStatus ?? (attributes.OrderStatus ?? "paid"),
                PaymentDate = attributes.CreatedAt ?? DateTime.UtcNow,
                CustomerEmail = attributes.CustomerEmail,
                Description = "Pro Subscription Payment",
                ReceiptUrl = receiptUrl
            };

            await _paymentService.UpsertAsync(payment);
            _logger.LogInformation("Upserted payment for order {OrderId}", orderId);
        }

        /// <summary>
        /// Health check endpoint for webhook URL validation
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}