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

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ILemonsqueezyService lemonsqueezyService,
            ITenantContext tenantContext,
            IUserService userService,
            IOrganizationUnitService organizationUnitService,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _lemonsqueezyService = lemonsqueezyService;
            _tenantContext = tenantContext;
            _userService = userService;
            _organizationUnitService = organizationUnitService;
            _logger = logger;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating checkout URL for organization {OrganizationUnitId}", 
                    _tenantContext.CurrentTenantId);
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

                // Get current user information to check trial eligibility
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool isEligibleForTrial = false;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    isEligibleForTrial = await _subscriptionService.IsOrganizationUnitEligibleForTrialAsync(organizationUnitId, userId);
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
                    IsEligibleForTrial = isEligibleForTrial
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
        public bool IsEligibleForTrial { get; set; }
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
}