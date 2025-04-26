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
        private readonly IConfiguration _configuration;

        public EmailTemplateService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetVerificationEmailTemplateAsync(string userName, string verificationLink, int tokenValidityHours)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Verify Your Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ max-width: 150px; height: auto; }}
        .button {{ display: inline-block; background-color: #3498db; color: white; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #999; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <img src='{_configuration["AppUrl"]}/logo.png' alt='OpenAutomate Logo' class='logo'>
        <h1>Verify Your Email Address</h1>
    </div>
    
    <p>Hello {userName},</p>
    
    <p>Thank you for registering with OpenAutomate! To complete your registration and activate your account, please verify your email address by clicking the button below:</p>
    
    <p style='text-align: center; margin: 30px 0;'>
        <a href='{verificationLink}' class='button'>Verify My Email</a>
    </p>
    
    <p>This verification link will expire in {tokenValidityHours} hours.</p>
    
    <p>If you did not create an account with OpenAutomate, please ignore this email.</p>
    
    <p>If you're having trouble clicking the button, copy and paste the URL below into your web browser:</p>
    <p style='word-break: break-all;'>{verificationLink}</p>
    
    <div class='footer'>
        <p>&copy; {DateTime.UtcNow.Year} OpenAutomate. All rights reserved.</p>
        <p>This is an automated message, please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        public async Task<string> GetWelcomeEmailTemplateAsync(string userName, string loginLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Welcome to OpenAutomate</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ max-width: 150px; height: auto; }}
        .button {{ display: inline-block; background-color: #3498db; color: white; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; }}
        .features {{ margin: 30px 0; }}
        .feature {{ margin-bottom: 15px; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #999; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <img src='{_configuration["AppUrl"]}/logo.png' alt='OpenAutomate Logo' class='logo'>
        <h1>Welcome to OpenAutomate!</h1>
    </div>
    
    <p>Hello {userName},</p>
    
    <p>Thank you for joining OpenAutomate! Your email has been successfully verified, and your account is now active.</p>
    
    <p style='text-align: center; margin: 30px 0;'>
        <a href='{loginLink}' class='button'>Log In to Your Account</a>
    </p>
    
    <div class='features'>
        <h2>Getting Started:</h2>
        <div class='feature'>
            <h3>1. Set Up Your Profile</h3>
            <p>Complete your profile information to personalize your experience.</p>
        </div>
        <div class='feature'>
            <h3>2. Create Your First Project</h3>
            <p>Start automating by creating your first project in just a few clicks.</p>
        </div>
        <div class='feature'>
            <h3>3. Explore Documentation</h3>
            <p>Check out our comprehensive documentation to learn more about our platform's capabilities.</p>
        </div>
    </div>
    
    <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
    
    <div class='footer'>
        <p>&copy; {DateTime.UtcNow.Year} OpenAutomate. All rights reserved.</p>
        <p>This is an automated message, please do not reply to this email.</p>
    </div>
</body>
</html>";
        }

        public async Task<string> GetInvitationEmailTemplateAsync(string userName, string inviterName, 
            string organizationName, string invitationLink, int tokenValidityHours, bool isExistingUser)
        {
            string registrationText = isExistingUser 
                ? "Sign in to your existing account to join this organization." 
                : "You'll need to create an account to join this organization.";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>You've Been Invited to Join {organizationName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ max-width: 150px; height: auto; }}
        .button {{ display: inline-block; background-color: #3498db; color: white; text-decoration: none; padding: 12px 24px; border-radius: 4px; font-weight: bold; }}
        .org-info {{ margin: 30px 0; padding: 15px; background-color: #f8f9fa; border-radius: 4px; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #999; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <img src='{_configuration["AppUrl"]}/logo.png' alt='OpenAutomate Logo' class='logo'>
        <h1>You've Been Invited!</h1>
    </div>
    
    <p>Hello {userName},</p>
    
    <p><strong>{inviterName}</strong> has invited you to join <strong>{organizationName}</strong> on OpenAutomate.</p>
    
    <div class='org-info'>
        <h3>Organization: {organizationName}</h3>
        <p>Invited by: {inviterName}</p>
        <p>{registrationText}</p>
    </div>
    
    <p style='text-align: center; margin: 30px 0;'>
        <a href='{invitationLink}' class='button'>Accept Invitation</a>
    </p>
    
    <p>This invitation will expire in {tokenValidityHours / 24} days.</p>
    
    <p>If you're having trouble clicking the button, copy and paste the URL below into your web browser:</p>
    <p style='word-break: break-all;'>{invitationLink}</p>
    
    <div class='footer'>
        <p>&copy; {DateTime.UtcNow.Year} OpenAutomate. All rights reserved.</p>
        <p>This is an automated message, please do not reply to this email.</p>
    </div>
</body>
</html>";
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously 