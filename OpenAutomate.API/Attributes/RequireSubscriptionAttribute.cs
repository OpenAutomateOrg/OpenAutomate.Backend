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
    /// Requires an active subscription (trial or paid) for authorization
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class RequireSubscriptionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _allowTrial;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireSubscriptionAttribute"/> class
        /// </summary>
        /// <param name="allowTrial">Whether to allow trial subscriptions (default: true)</param>
        public RequireSubscriptionAttribute(bool allowTrial = true)
        {
            _allowTrial = allowTrial;
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

                // Determine if access should be granted
                bool hasAccess = false;
                string denialReason = "";

                if (!subscriptionStatus.HasSubscription)
                {
                    denialReason = "No subscription found. Please start your free trial or upgrade to premium.";
                }
                else if (!subscriptionStatus.IsActive)
                {
                    denialReason = subscriptionStatus.Status switch
                    {
                        "expired" => "Your subscription has expired. Please renew your subscription to continue using premium features.",
                        "cancelled" => "Your subscription has been cancelled. Please reactivate your subscription to continue using premium features.",
                        _ => "Your subscription is not active. Please check your subscription status."
                    };
                }
                else
                {
                    // Subscription is active, check if it's trial and if trials are allowed
                    if (subscriptionStatus.IsInTrial && !_allowTrial)
                    {
                        denialReason = "This feature requires a paid subscription. Please upgrade from your trial.";
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