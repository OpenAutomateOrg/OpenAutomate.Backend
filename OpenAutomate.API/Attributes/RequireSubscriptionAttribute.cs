using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
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
                var user = context.HttpContext.Items["User"] as User;

                if (user == null)
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "User not authenticated" });
                    return;
                }

                // Get the tenant context to determine the current organization
                var tenantContext = context.HttpContext.RequestServices
                    .GetRequiredService<ITenantContext>();

                if (!tenantContext.HasTenant)
                {
                    context.Result = new BadRequestObjectResult(new { message = "Tenant context not available" });
                    return;
                }

                // Get the subscription service
                var subscriptionService = context.HttpContext.RequestServices
                    .GetRequiredService<ISubscriptionService>();

                // Check subscription status
                var subscriptionStatus = await subscriptionService.GetSubscriptionStatusAsync(tenantContext.CurrentTenantId);

                // Determine if access should be granted based on operation type
                bool hasAccess = false;
                string denialReason = "";

                if (!subscriptionStatus.HasSubscription)
                {
                    denialReason = "No subscription found. Please start your free trial or upgrade to Standard subscription.";
                }
                else if (!subscriptionStatus.IsActive)
                {
                    // For expired/cancelled subscriptions, allow read operations if it was a trial
                    if (_operationType == SubscriptionOperationType.Read && 
                        (subscriptionStatus.Status == "expired" && subscriptionStatus.TrialEndsAt.HasValue))
                    {
                        hasAccess = true; // Allow read access for expired trials
                    }
                    else if (_operationType == SubscriptionOperationType.Write)
                    {
                        denialReason = subscriptionStatus.Status switch
                        {
                            "expired" => "Your trial has expired. Please upgrade to Standard subscription to create, modify, or delete resources.",
                            "cancelled" => "Your subscription has been cancelled. Please reactivate your subscription to create, modify, or delete resources.",
                            _ => "Your subscription is not active. Please upgrade to Standard subscription for full access."
                        };
                    }
                    else
                    {
                        denialReason = subscriptionStatus.Status switch
                        {
                            "expired" => "Your subscription has expired. Please renew your subscription to continue using premium features.",
                            "cancelled" => "Your subscription has been cancelled. Please reactivate your subscription to continue using premium features.",
                            _ => "Your subscription is not active. Please check your subscription status."
                        };
                    }
                }
                else
                {
                    // Subscription is active, check if it's trial and if trials are allowed for write operations
                    if (subscriptionStatus.IsInTrial && !_allowTrial && _operationType == SubscriptionOperationType.Write)
                    {
                        denialReason = "This feature requires a Standard subscription. Please upgrade from your trial.";
                    }
                    else
                    {
                        hasAccess = true;
                    }
                }

                if (!hasAccess)
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

                    context.Result = new ObjectResult(response)
                    {
                        StatusCode = 402 // Payment Required
                    };
                    return;
                }

                // Access granted - continue to the action
                await next();
            }
            catch (Exception ex)
            {
                // Log the error (get logger from DI)
                var logger = context.HttpContext.RequestServices
                    .GetService<ILogger<RequireSubscriptionAttribute>>();
                    
                logger?.LogError(ex, "Error checking subscription requirements");

                // Return a generic error to avoid exposing internal details
                context.Result = new ObjectResult(new { message = "Unable to verify subscription status" })
                {
                    StatusCode = 500
                };
            }
        }
    }
}