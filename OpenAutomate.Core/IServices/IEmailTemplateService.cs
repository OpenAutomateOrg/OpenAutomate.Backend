using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for generating email templates
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Gets the email verification template
        /// </summary>
        /// <param name="userName">The user's name</param>
        /// <param name="verificationLink">The verification link</param>
        /// <param name="tokenValidityHours">The validity period in hours</param>
        /// <returns>The HTML email content</returns>
        Task<string> GetVerificationEmailTemplateAsync(string userName, string verificationLink, int tokenValidityHours);
        
        /// <summary>
        /// Gets the welcome email template
        /// </summary>
        /// <param name="userName">The user's name</param>
        /// <param name="loginLink">The login link</param>
        /// <returns>The HTML email content</returns>
        Task<string> GetWelcomeEmailTemplateAsync(string userName, string loginLink);
        
        /// <summary>
        /// Gets the organization invitation email template
        /// </summary>
        /// <param name="userName">The recipient's name</param>
        /// <param name="inviterName">The inviter's name</param>
        /// <param name="organizationName">The organization name</param>
        /// <param name="invitationLink">The invitation link</param>
        /// <param name="tokenValidityHours">The validity period in hours</param>
        /// <param name="isExistingUser">Whether the recipient is an existing user</param>
        /// <returns>The HTML email content</returns>
        Task<string> GetInvitationEmailTemplateAsync(string userName, string inviterName, 
            string organizationName, string invitationLink, int tokenValidityHours, bool isExistingUser);
            
        /// <summary>
        /// Gets the password reset email template
        /// </summary>
        /// <param name="userName">The user's name</param>
        /// <param name="resetLink">The password reset link</param>
        /// <param name="tokenValidityHours">The validity period in hours</param>
        /// <returns>The HTML email content</returns>
        Task<string> GetResetPasswordEmailTemplateAsync(string userName, string resetLink, int tokenValidityHours);

        /// <summary>
        /// Gets the execution completion email template
        /// </summary>
        /// <param name="userName">The user's name</param>
        /// <param name="packageName">The package name</param>
        /// <param name="status">The execution status</param>
        /// <param name="startTime">The execution start time</param>
        /// <param name="endTime">The execution end time</param>
        /// <param name="duration">The execution duration</param>
        /// <param name="errorMessage">The error message (if any)</param>
        /// <returns>The HTML email content</returns>
        Task<string> GetExecutionCompletionEmailTemplateAsync(string userName, string packageName, string status, DateTime startTime, DateTime? endTime, string duration, string? errorMessage = null);
    }
} 