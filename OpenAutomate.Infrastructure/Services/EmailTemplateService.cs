#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public EmailTemplateService()
        {
        }

        #region Public Email Template Methods

        public Task<string> GetVerificationEmailTemplateAsync(string userName, string verificationLink, int tokenValidityHours)
        {
            string content = $@"
    <p>Hello {userName},</p>
    
    <p>To verify your account, please click the URL below:</p>
    
    <p><a href='{verificationLink}'>{verificationLink}</a></p>
    
    <p>This link will expire in {tokenValidityHours} hours.</p>";

            return Task.FromResult(WrapInEmailTemplate("Verify Your Email", "Verify Your Email Address", content));
        }

        public Task<string> GetWelcomeEmailTemplateAsync(string userName, string loginLink)
        {
            string content = $@"
    <p>Hello {userName},</p>
    
    <p>Your email has been verified successfully. To login to your account, please click the URL below:</p>
    
    <p><a href='{loginLink}'>{loginLink}</a></p>";

            return Task.FromResult(WrapInEmailTemplate("Welcome to OpenAutomate", "Welcome to OpenAutomate", content));
        }

        public Task<string> GetInvitationEmailTemplateAsync(string userName, string inviterName, 
            string organizationName, string invitationLink, int tokenValidityHours, bool isExistingUser)
        {
            string content = $@"
    <p>Hello {userName},</p>
    
    <p>{inviterName} has invited you to join {organizationName}. To accept this invitation, please click the URL below:</p>
    
    <p><a href='{invitationLink}'>{invitationLink}</a></p>
    
    <p>This invitation will expire in {tokenValidityHours / 24} days.</p>";

            return Task.FromResult(WrapInEmailTemplate($"Invitation to Join {organizationName}", $"Invitation to Join {organizationName}", content));
        }

        public Task<string> GetResetPasswordEmailTemplateAsync(string userName, string resetLink, int tokenValidityHours)
        {
            string content = $@"
    <p>Hello {userName},</p>
    
    <p>To reset your password, please click the URL below:</p>
    
    <p><a href='{resetLink}'>{resetLink}</a></p>
    
    <p>This link will expire in {tokenValidityHours} hours.</p>";

            return Task.FromResult(WrapInEmailTemplate("Reset Your Password", "Reset Your Password", content));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Wraps the email content in a common HTML template with styles
        /// </summary>
        /// <param name="title">The email title (used in the HTML title tag)</param>
        /// <param name="heading">The main heading displayed in the email</param>
        /// <param name="content">The main content of the email</param>
        /// <returns>Complete HTML email template</returns>
        private string WrapInEmailTemplate(string title, string heading, string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{title}</title>
    <style>
        {GetCommonStyles()}
    </style>
</head>
<body>
    <div class='header'>
        <h2>{heading}</h2>
    </div>
    
    {content}
    
    <div class='footer'>
        <p>&copy; {DateTime.UtcNow.Year} OpenAutomate</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Gets the common CSS styles used across all email templates
        /// </summary>
        private string GetCommonStyles()
        {
            return @"
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; margin-bottom: 20px; }
        .header h2 { color: #ea580c; }
        .footer { margin-top: 20px; font-size: 12px; color: #999; text-align: center; }
        a { color: #ea580c; }";
        }

        #endregion
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously 