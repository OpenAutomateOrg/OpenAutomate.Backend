using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(Guid userId, string email, string name)
        {
            try
            {
                // Generate verification token
                var token = await _tokenService.GenerateEmailVerificationTokenAsync(userId);
                
                // Create verification link
                var baseUrl = _configuration["FrontendUrl"];
                var verificationLink = $"{baseUrl}/email/verify?token={token}";
                
                // Get email template
                var emailContent = await _emailTemplateService.GetVerificationEmailTemplateAsync(
                    name, verificationLink, 24); // 24 hours validity
                
                // Send email
                string subject = "Verify Your Email Address - OpenAutomate";
                await _emailService.SendEmailAsync(email, subject, emailContent);
                
                _logger.LogInformation("Verification email sent to: {Email} for user: {UserId}", email, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to: {Email} for user: {UserId}", email, userId);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            try
            {
                // Create login link
                var baseUrl = _configuration["FrontendUrl"];
                var loginLink = $"{baseUrl}/login";
                
                // Get email template
                var emailContent = await _emailTemplateService.GetWelcomeEmailTemplateAsync(
                    name, loginLink);
                
                // Send email
                string subject = "Welcome to OpenAutomate";
                await _emailService.SendEmailAsync(email, subject, emailContent);
                
                _logger.LogInformation("Welcome email sent to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to: {Email}", email);
                throw;
            }
        }

        public async Task SendOrganizationUnitInvitationAsync(Guid inviterId, string recipientEmail, Guid organizationId, string invitationToken, DateTime expiresAt, bool isExistingUser)
        {
            try
            {
                // Get inviter and organization info
                var inviter = await _unitOfWork.Users.GetByIdAsync(inviterId);
                var organization = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationId);

                if (inviter == null || organization == null)
                {
                    _logger.LogWarning("Failed to send invitation: Inviter or organization not found");
                    throw new Exception("Inviter or organization not found");
                }

                // Create invitation link
                var baseUrl = _configuration["FrontendUrl"];
                var invitationLink = $"{baseUrl}/{organization.Slug}/invitation/accept?token={invitationToken}";

                // Get email template
                var emailContent = await _emailTemplateService.GetInvitationEmailTemplateAsync(
                    recipientEmail,
                    $"{inviter.FirstName ?? ""} {inviter.LastName ?? ""}",
                    organization.Name ?? "Organization",
                    invitationLink,
                    (int)(expiresAt - DateTime.UtcNow).TotalHours,
                    isExistingUser);

                // Send email
                string subject = $"You've Been Invited to Join {organization.Name ?? "Organization"} on OpenAutomate";
                await _emailService.SendEmailAsync(recipientEmail, subject, emailContent);

                _logger.LogInformation("Invitation email sent to: {Email} for organization: {OrgId}",
                    recipientEmail, organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to: {Email} for organization: {OrgId}",
                    recipientEmail, organizationId);
                throw;
            }
        }

        public async Task SendResetPasswordEmailAsync(string email, string resetLink)
        {
            try
            {
                // Find user by email to get their name
                var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
                
                if (user == null)
                {
                    _logger.LogWarning("Failed to send reset password email: User not found with email {Email}", email);
                    throw new Exception($"User not found with email {email}");
                }
                
                string name = $"{user.FirstName ?? ""} {user.LastName ?? ""}";
                
                // Get email template - use 4 hours to match token expiration
                var emailContent = await _emailTemplateService.GetResetPasswordEmailTemplateAsync(
                    name, resetLink, 4); // 4 hour validity
                
                // Send email
                string subject = "Reset Your Password - OpenAutomate";
                await _emailService.SendEmailAsync(email, subject, emailContent);
                
                _logger.LogInformation("Reset password email sent to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset password email to: {Email}", email);
                throw;
            }
        }

        public async Task SendExecutionCompletionEmailAsync(Guid creatorId, Guid executionId, string packageName, string status, DateTime startTime, DateTime? endTime, string? errorMessage = null)
        {
            try
            {
                // Get the execution creator
                var creator = await _unitOfWork.Users.GetByIdAsync(creatorId);
                if (creator == null)
                {
                    _logger.LogWarning("Failed to send execution completion email: User not found with ID {CreatorId}", creatorId);
                    return;
                }

                if (string.IsNullOrEmpty(creator.Email))
                {
                    _logger.LogWarning("Failed to send execution completion email: User {CreatorId} has no email address", creatorId);
                    return;
                }

                string userName = $"{creator.FirstName ?? ""} {creator.LastName ?? ""}".Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = creator.Email;
                }

                // Calculate duration
                string duration = "N/A";
                if (endTime.HasValue)
                {
                    var timeSpan = endTime.Value - startTime;
                    if (timeSpan.TotalDays >= 1)
                    {
                        duration = $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
                    }
                    else if (timeSpan.TotalHours >= 1)
                    {
                        duration = $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
                    }
                    else if (timeSpan.TotalMinutes >= 1)
                    {
                        duration = $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
                    }
                    else
                    {
                        duration = $"{timeSpan.Seconds}s";
                    }
                }

                // Get email template
                var emailContent = await _emailTemplateService.GetExecutionCompletionEmailTemplateAsync(
                    userName, packageName, status, startTime, endTime, duration, errorMessage);

                // Send email
                var isSuccess = status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
                string subject = isSuccess 
                    ? $"Execution Completed Successfully - {packageName} - OpenAutomate"
                    : $"Execution {status} - {packageName} - OpenAutomate";

                await _emailService.SendEmailAsync(creator.Email, subject, emailContent);

                _logger.LogInformation("Execution completion email sent to: {Email} for execution: {ExecutionId}, status: {Status}", 
                    creator.Email, executionId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send execution completion email for execution: {ExecutionId}, creator: {CreatorId}", 
                    executionId, creatorId);
                // Don't throw - we don't want email failures to affect execution status updates
            }
        }
    }
} 