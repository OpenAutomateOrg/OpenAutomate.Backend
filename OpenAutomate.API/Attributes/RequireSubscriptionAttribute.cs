using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.API.Attributes
{
    /// <summary>
    /// Operation types for subscription requirements
    /// </summary>
    public enum SubscriptionOperationType
    {
        /// <summary>
        /// Read operations (GET requests) - allowed for expired trials
        /// </summary>
        Read,
        /// <summary>
        /// Write operations (POST, PUT, DELETE, PATCH) - requires active subscription
        /// </summary>
        Write
    }

    /// <summary>
    /// Result of subscription access determination
    /// </summary>
    public record AccessResult(bool HasAccess, string DenialReason);

    /// <summary>
    /// Requires an active subscription (trial or paid) for authorization
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class RequireSubscriptionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _allowTrial;
        private readonly SubscriptionOperationType _operationType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireSubscriptionAttribute"/> class
        /// </summary>
        /// <param name="allowTrial">Whether to allow trial subscriptions (default: true)</param>
        public RequireSubscriptionAttribute(bool allowTrial = true)
        {
            _allowTrial = allowTrial;
            _operationType = SubscriptionOperationType.Write; // Default to more restrictive
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireSubscriptionAttribute"/> class with operation type
        /// </summary>
        /// <param name="operationType">The type of operation (Read allows expired trials, Write requires active subscription)</param>
        /// <param name="allowTrial">Whether to allow trial subscriptions for write operations (default: true)</param>
        public RequireSubscriptionAttribute(SubscriptionOperationType operationType, bool allowTrial = true)
        {
            _allowTrial = allowTrial;
            _operationType = operationType;
        }

        /// <summary>
        /// Executes the subscription check before the action executes
        /// </summary>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var (user, tenantContext, validationError) = ValidateUserAndTenant(context);
                if (validationError != null)
                {
                    context.Result = validationError;
                    return;
                }

                var subscriptionService = context.HttpContext.RequestServices
                    .GetRequiredService<ISubscriptionService>();

                var subscriptionStatus = await subscriptionService.GetSubscriptionStatusAsync(tenantContext.CurrentTenantId);
                var accessResult = DetermineSubscriptionAccess(subscriptionStatus);

                if (!accessResult.HasAccess)
                {
                    context.Result = BuildPaymentRequiredResponse(accessResult.DenialReason, subscriptionStatus);
                    return;
                }

                await next();
            }
            catch (Exception ex)
            {
                HandleException(context, ex);
            }
        }

        /// <summary>
        /// Validates user authentication and tenant context
        /// </summary>
        private static (User user, ITenantContext tenantContext, IActionResult errorResult) ValidateUserAndTenant(ActionExecutingContext context)
        {
            var user = context.HttpContext.Items["User"] as User;
            if (user == null)
            {
                return (null, null, new UnauthorizedObjectResult(new { message = "User not authenticated" }));
            }

            var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
            if (!tenantContext.HasTenant)
            {
                return (null, null, new BadRequestObjectResult(new { message = "Tenant context not available" }));
            }

            return (user, tenantContext, null);
        }

        /// <summary>
        /// Determines if access should be granted based on subscription status and operation type
        /// </summary>
        private AccessResult DetermineSubscriptionAccess(SubscriptionStatus subscriptionStatus)
        {
            if (!subscriptionStatus.HasSubscription)
            {
                return new AccessResult(false, "No subscription found. Please start your free trial or upgrade to Standard subscription.");
            }

            if (!subscriptionStatus.IsActive)
            {
                return HandleInactiveSubscription(subscriptionStatus);
            }

            return HandleActiveSubscription(subscriptionStatus);
        }

        /// <summary>
        /// Handles access determination for inactive subscriptions
        /// </summary>
        private AccessResult HandleInactiveSubscription(SubscriptionStatus subscriptionStatus)
        {
            // Allow read operations for expired trials
            if (_operationType == SubscriptionOperationType.Read &&
                subscriptionStatus.Status == "expired" &&
                subscriptionStatus.TrialEndsAt.HasValue)
            {
                return new AccessResult(true, string.Empty);
            }

            var denialReason = GetInactiveSubscriptionDenialReason(subscriptionStatus.Status);
            return new AccessResult(false, denialReason);
        }

        /// <summary>
        /// Handles access determination for active subscriptions
        /// </summary>
        private AccessResult HandleActiveSubscription(SubscriptionStatus subscriptionStatus)
        {
            if (subscriptionStatus.IsInTrial &&
                !_allowTrial &&
                _operationType == SubscriptionOperationType.Write)
            {
                return new AccessResult(false, "This feature requires a Standard subscription. Please upgrade from your trial.");
            }

            return new AccessResult(true, string.Empty);
        }

        /// <summary>
        /// Gets the appropriate denial reason for inactive subscriptions
        /// </summary>
        private string GetInactiveSubscriptionDenialReason(string status)
        {
            if (_operationType == SubscriptionOperationType.Write)
            {
                return status switch
                {
                    "expired" => "Your trial has expired. Please upgrade to Standard subscription to create, modify, or delete resources.",
                    "cancelled" => "Your subscription has been cancelled. Please reactivate your subscription to create, modify, or delete resources.",
                    _ => "Your subscription is not active. Please upgrade to Standard subscription for full access."
                };
            }

            return status switch
            {
                "expired" => "Your subscription has expired. Please renew your subscription to continue using premium features.",
                "cancelled" => "Your subscription has been cancelled. Please reactivate your subscription to continue using premium features.",
                _ => "Your subscription is not active. Please check your subscription status."
            };
        }

        /// <summary>
        /// Builds the payment required response with subscription details
        /// </summary>
        private ObjectResult BuildPaymentRequiredResponse(string denialReason, SubscriptionStatus subscriptionStatus)
        {
            var response = new
            {
                message = denialReason,
                subscriptionStatus = new
                {
                    hasSubscription = subscriptionStatus.HasSubscription,
                    isActive = subscriptionStatus.IsActive,
                    isInTrial = subscriptionStatus.IsInTrial,
                    status = subscriptionStatus.Status,
                    planName = subscriptionStatus.PlanName,
                    trialEndsAt = subscriptionStatus.TrialEndsAt,
                    renewsAt = subscriptionStatus.RenewsAt,
                    daysRemaining = subscriptionStatus.DaysRemaining,
                    upgradeRequired = !_allowTrial && subscriptionStatus.IsInTrial
                }
            };

            return new ObjectResult(response)
            {
                StatusCode = 402 // Payment Required
            };
        }

        /// <summary>
        /// Handles exceptions that occur during subscription checking
        /// </summary>
        private static void HandleException(ActionExecutingContext context, Exception ex)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<RequireSubscriptionAttribute>>();

            logger?.LogError(ex, "Error checking subscription requirements");

            context.Result = new ObjectResult(new { message = "Unable to verify subscription status" })
            {
                StatusCode = 500
            };
        }
    }
}