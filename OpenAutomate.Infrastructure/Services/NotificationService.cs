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

        public async Task SendOrganizationInvitationAsync(Guid inviterId, string recipientEmail, 
            string recipientName, Guid organizationId, bool isExistingUser)
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
                
                // Generate invitation token
                // This would be implemented in a real invitation service
                var invitationToken = "sample-invitation-token"; // Placeholder
                
                // Create invitation link
                var baseUrl = _configuration["FrontendUrl"];
                var invitationLink = $"{baseUrl}/invitation?token={invitationToken}";
                
                // Get email template
                var emailContent = await _emailTemplateService.GetInvitationEmailTemplateAsync(
                    recipientName,
                    $"{inviter.FirstName ?? ""} {inviter.LastName ?? ""}",
                    organization.Name ?? "Organization",
                    invitationLink,
                    168, // 7 days validity
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
    }
} 