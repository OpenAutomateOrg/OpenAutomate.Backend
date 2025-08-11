using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    /// <summary>
    /// Controller for managing subscriptions and checkout processes
    /// </summary>
    [ApiController]
    [Route("{tenant}/api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILemonsqueezyService _lemonsqueezyService;
        private readonly ITenantContext _tenantContext;
        private readonly IUserService _userService;
        private readonly IOrganizationUnitService _organizationUnitService;
        private readonly ILogger<SubscriptionController> _logger;
        private readonly IHostEnvironment _env;
        private readonly IPaymentService _paymentService;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ILemonsqueezyService lemonsqueezyService,
            ITenantContext tenantContext,
            IUserService userService,
            IOrganizationUnitService organizationUnitService,
            IPaymentService paymentService,
            ILogger<SubscriptionController> logger,
            IHostEnvironment env)
        {
            _subscriptionService = subscriptionService;
            _lemonsqueezyService = lemonsqueezyService;
            _tenantContext = tenantContext;
            _userService = userService;
            _organizationUnitService = organizationUnitService;
            _logger = logger;
            _paymentService = paymentService;
            _env = env;
        }

        /// <summary>
        /// Generates a Lemon Squeezy checkout URL for the current tenant
        /// </summary>
        /// <param name="redirectUrl">Optional redirect URL after successful payment</param>
        /// <returns>Checkout URL for the user to complete payment</returns>
        [HttpGet("checkout")]
        [ProducesResponseType(typeof(CheckoutResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCheckoutUrl([FromQuery] string? redirectUrl = null)
        {
            try
            {
                // Validate tenant context
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning("Checkout request without valid tenant context");
                    return BadRequest(new { message = "Invalid tenant context" });
                }

                var organizationUnitId = _tenantContext.CurrentTenantId;

                // Get current user information
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Checkout request without valid user ID");
                    return BadRequest(new { message = "Invalid user context" });
                }

                var user = await _userService.GetByIdAsync(userGuid);
                if (user == null)
                {
                    _logger.LogWarning("User not found for checkout: {UserId}", userGuid);
                    return BadRequest(new { message = "User not found" });
                }

                _logger.LogInformation("Generating checkout URL for user {UserId} in organization {OrganizationUnitId}", 
                    userGuid, organizationUnitId);

                // Generate checkout URL with Lemon Squeezy
                var checkoutUrl = await _lemonsqueezyService.CreateCheckoutUrlAsync(
                    organizationUnitId, 
                    user.Email, 
                    redirectUrl);

                _logger.LogInformation("Successfully generated checkout URL for organization {OrganizationUnitId}", 
                    organizationUnitId);

                return Ok(new CheckoutResponse
                {
                    CheckoutUrl = checkoutUrl,
                    OrganizationUnitId = organizationUnitId,
                    UserEmail = user.Email
                });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error generating checkout URL for organization {OrganizationUnitId}", 
                    _tenantContext.CurrentTenantId);
                if (_env.IsDevelopment())
                {
                    return StatusCode(502, new { 
                        message = "Vendor error while generating checkout URL",
                        details = httpEx.Message
                    });
                }
                return StatusCode(500, new { message = "An error occurred while generating checkout URL" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating checkout URL for organization {OrganizationUnitId}",
                    _tenantContext.CurrentTenantId);
                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new { message = ex.Message });
                }
                return StatusCode(500, new { message = "An error occurred while generating checkout URL" });
            }
        }

        /// <summary>
        /// Gets the current subscription status for the tenant
        /// </summary>
        /// <returns>Current subscription status and details</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(SubscriptionStatusResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                if (!_tenantContext.HasTenant)
                {
                    return BadRequest(new { message = "Invalid tenant context" });
                }

                var organizationUnitId = _tenantContext.CurrentTenantId;
                var subscriptionStatus = await _subscriptionService.GetSubscriptionStatusAsync(organizationUnitId);

                // Get current user information to determine trial status
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                TrialStatus userTrialStatus = TrialStatus.NotEligible;
                
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
                {
                    userTrialStatus = await DetermineUserTrialStatusAsync(organizationUnitId, userGuid, subscriptionStatus);
                }

                return Ok(new SubscriptionStatusResponse
                {
                    HasSubscription = subscriptionStatus.HasSubscription,
                    IsActive = subscriptionStatus.IsActive,
                    IsInTrial = subscriptionStatus.IsInTrial,
                    Status = subscriptionStatus.Status,
                    PlanName = subscriptionStatus.PlanName,
                    TrialEndsAt = subscriptionStatus.TrialEndsAt,
                    RenewsAt = subscriptionStatus.RenewsAt,
                    DaysRemaining = subscriptionStatus.DaysRemaining,
                    OrganizationUnitId = organizationUnitId,
                    UserTrialStatus = userTrialStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for organization {OrganizationUnitId}", 
                    _tenantContext.CurrentTenantId);
                return StatusCode(500, new { message = "An error occurred while getting subscription status" });
            }
        }

        /// <summary>
        /// Starts a trial subscription for the current organization unit
        /// </summary>
        /// <returns>Response indicating whether trial was started successfully</returns>
        [HttpPost("start-trial")]
        [ProducesResponseType(typeof(StartTrialResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)] // Conflict - trial already exists
        public async Task<IActionResult> StartTrial()
        {
            try
            {
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning("Start trial request without valid tenant context");
                    return BadRequest(new { message = "Invalid tenant context" });
                }

                var organizationUnitId = _tenantContext.CurrentTenantId;

                // Get current user information
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Start trial request without valid user ID");
                    return BadRequest(new { message = "Invalid user context" });
                }

                // Verify the Organization Unit exists
                var organizationUnit = await _organizationUnitService.GetOrganizationUnitByIdAsync(organizationUnitId);
                if (organizationUnit == null)
                {
                    _logger.LogWarning("Organization Unit {OrganizationUnitId} not found", organizationUnitId);
                    return BadRequest(new { message = "Organization Unit not found" });
                }

                _logger.LogInformation("Starting trial for organization {OrganizationUnitId} (slug: {Slug}) by user {UserId}", 
                    organizationUnitId, organizationUnit.Slug, userGuid);

                // Check if subscription already exists
                var existingStatus = await _subscriptionService.GetSubscriptionStatusAsync(organizationUnitId);
                if (existingStatus.HasSubscription)
                {
                    _logger.LogWarning("Attempted to start trial for organization {OrganizationUnitId} that already has a subscription", 
                        organizationUnitId);
                    return Conflict(new { message = "Trial or subscription already exists for this organization" });
                }

                // Start the trial
                var result = await _subscriptionService.StartTrialSubscriptionAsync(organizationUnitId, userGuid.ToString());

                if (result)
                {
                    _logger.LogInformation("Successfully started trial for organization {OrganizationUnitId}", 
                        organizationUnitId);

                    // Get the updated subscription status
                    var newStatus = await _subscriptionService.GetSubscriptionStatusAsync(organizationUnitId);

                    return Ok(new StartTrialResponse
                    {
                        Success = true,
                        Message = "Trial started successfully",
                        TrialEndsAt = newStatus.TrialEndsAt,
                        OrganizationUnitId = organizationUnitId
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to start trial for organization {OrganizationUnitId} - user {UserId} may have already used a trial", organizationUnitId, userGuid);
                    return BadRequest(new { message = "Failed to start trial subscription. You may have already used your free trial." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting trial for organization {OrganizationUnitId}", 
                    _tenantContext.CurrentTenantId);
                return StatusCode(500, new { message = "An error occurred while starting the trial" });
            }
        }

        /// <summary>
        /// Gets the customer portal URL for managing subscription
        /// </summary>
        /// <returns>Customer portal URL from Lemon Squeezy</returns>
        [HttpGet("customer-portal")]
        [ProducesResponseType(typeof(CustomerPortalResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCustomerPortal()
        {
            try
            {
                if (!_tenantContext.HasTenant)
                {
                    _logger.LogWarning("Customer portal request without valid tenant context");
                    return BadRequest(new { message = "Invalid tenant context" });
                }

                var organizationUnitId = _tenantContext.CurrentTenantId;

                // Get subscription for current organization
                var subscription = await _subscriptionService.GetSubscriptionByOrganizationUnitIdAsync(organizationUnitId);
                
                if (subscription?.LemonsqueezySubscriptionId == null)
                {
                    _logger.LogWarning("No active subscription found for organization {OrganizationUnitId}", organizationUnitId);
                    return NotFound(new { message = "No active subscription found" });
                }

                _logger.LogInformation("Getting customer portal URL for subscription {SubscriptionId}", subscription.LemonsqueezySubscriptionId);

                string portalUrl;
                try
                {
                    // Prefer fresh URL from Lemon Squeezy API
                    portalUrl = await _lemonsqueezyService.GetCustomerPortalUrlAsync(subscription.LemonsqueezySubscriptionId);
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogWarning(httpEx, "Falling back to cached portal URL for organization {OrganizationUnitId}", organizationUnitId);
                    if (!string.IsNullOrWhiteSpace(subscription.CustomerPortalUrl))
                    {
                        portalUrl = subscription.CustomerPortalUrl;
                    }
                    else
                    {
                        throw;
                    }
                }

                _logger.LogInformation("Successfully retrieved customer portal URL for organization {OrganizationUnitId}", organizationUnitId);

                return Ok(new CustomerPortalResponse
                {
                    Url = portalUrl,
                    OrganizationUnitId = organizationUnitId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer portal URL for organization {OrganizationUnitId}", 
                    _tenantContext.CurrentTenantId);
                return StatusCode(500, new { message = "An error occurred while getting customer portal URL" });
            }
        }

        /// <summary>
        /// List tenant billing payments (invoices/receipts) for display in UI
        /// </summary>
        [HttpGet("payments")]
        [ProducesResponseType(typeof(OpenAutomate.Core.Dto.Common.PagedResult<OpenAutomate.Core.IServices.PaymentDto>), 200)]
        public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            if (!_tenantContext.HasTenant)
            {
                return BadRequest(new { message = "Invalid tenant context" });
            }

            var result = await _paymentService.GetPaymentsAsync(_tenantContext.CurrentTenantId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Redirects to vendor-hosted invoice/receipt for a given order id
        /// </summary>
        [HttpGet("payments/{orderId}/view")]
        [ProducesResponseType(302)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RedirectToInvoice(string orderId)
        {
            if (!_tenantContext.HasTenant)
            {
                return BadRequest(new { message = "Invalid tenant context" });
            }

            var url = await _paymentService.GetReceiptUrlAsync(_tenantContext.CurrentTenantId, orderId);
            if (string.IsNullOrWhiteSpace(url))
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Safe external redirect: returning the URL allows FE to open in new tab; or we can 302
            return Redirect(url);
        }

        /// <summary>
        /// Determines the user's trial status based on current subscription and user's trial history
        /// </summary>
        /// <param name="organizationUnitId">The current organization unit ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="currentSubscriptionStatus">The current subscription status</param>
        /// <returns>The user's trial status</returns>
        private async Task<TrialStatus> DetermineUserTrialStatusAsync(Guid organizationUnitId, Guid userId, SubscriptionStatus currentSubscriptionStatus)
        {
            try
            {
                // If the OU has an active trial, return Active
                if (currentSubscriptionStatus.IsInTrial && currentSubscriptionStatus.IsActive)
                {
                    return TrialStatus.Active;
                }

                // If the OU has no subscription, check eligibility
                if (!currentSubscriptionStatus.HasSubscription)
                {
                    var isEligible = await _subscriptionService.IsOrganizationUnitEligibleForTrialAsync(organizationUnitId, userId.ToString());
                    return isEligible ? TrialStatus.Eligible : TrialStatus.Used;
                }

                // If OU has a subscription but it's not an active trial (expired trial or paid subscription)
                // Check if user has used trial elsewhere
                var userTrialSubscriptions = await _subscriptionService.GetSubscriptionByOrganizationUnitIdAsync(organizationUnitId);
                if (userTrialSubscriptions?.TrialEndsAt != null)
                {
                    // User had a trial on this OU
                    return TrialStatus.Used;
                }

                // Default case - subscription exists but no trial history found
                return TrialStatus.NotEligible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining trial status for user {UserId} in organization {OrganizationUnitId}", userId, organizationUnitId);
                return TrialStatus.NotEligible;
            }
        }
    }

    /// <summary>
    /// Enum representing the trial status for a user
    /// </summary>
    public enum TrialStatus
    {
        /// <summary>
        /// User is eligible to start a trial
        /// </summary>
        Eligible,
        
        /// <summary>
        /// User has an active trial
        /// </summary>
        Active,
        
        /// <summary>
        /// User has used their trial (on this or another organization unit)
        /// </summary>
        Used,
        
        /// <summary>
        /// User is not eligible for a trial
        /// </summary>
        NotEligible
    }

    /// <summary>
    /// Response model for checkout URL generation
    /// </summary>
    public class CheckoutResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public Guid OrganizationUnitId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for subscription status
    /// </summary>
    public class SubscriptionStatusResponse
    {
        public bool HasSubscription { get; set; }
        public bool IsActive { get; set; }
        public bool IsInTrial { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? RenewsAt { get; set; }
        public int? DaysRemaining { get; set; }
        public Guid OrganizationUnitId { get; set; }
        public TrialStatus UserTrialStatus { get; set; }
    }

    /// <summary>
    /// Response model for starting a trial subscription
    /// </summary>
    public class StartTrialResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? TrialEndsAt { get; set; }
        public Guid OrganizationUnitId { get; set; }
    }

    /// <summary>
    /// Response model for customer portal URL
    /// </summary>
    public class CustomerPortalResponse
    {
        public string Url { get; set; } = string.Empty;
        public Guid OrganizationUnitId { get; set; }
    }
}