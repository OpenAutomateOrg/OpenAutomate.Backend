using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Service for sending notifications to users
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a verification email to a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="email">The user's email address</param>
        /// <param name="name">The user's name</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendVerificationEmailAsync(Guid userId, string email, string name);
        
        /// <summary>
        /// Sends a welcome email to a user
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="name">The user's name</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendWelcomeEmailAsync(string email, string name);

        /// <summary>
        /// Sends an organization unit invitation email
        /// </summary>
        /// <param name="inviterId">The inviter's user ID</param>
        /// <param name="recipientEmail">The recipient's email address</param>
        /// <param name="organizationId">The organization ID</param>
        /// <param name="invitationToken">The invitation token</param>
        /// <param name="expiresAt">The expiration date of the invitation</param>
        /// <param name="isExistingUser">Whether the recipient is an existing user</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendOrganizationUnitInvitationAsync(Guid inviterId, string recipientEmail, Guid organizationId, string invitationToken, DateTime expiresAt, bool isExistingUser);

        /// <summary>
        /// Sends a password reset email to a user
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="resetLink">The password reset link</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task SendResetPasswordEmailAsync(string email, string resetLink);
    }
}