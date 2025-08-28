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
    <div class='email-container'>
        <div class='greeting'>
            <h3>Hello {userName},</h3>
            <p>Welcome to OpenAutomate! We're excited to have you join our automation platform.</p>
        </div>
        
        <div class='section'>
            <div class='info-box verification'>
                <div class='info-icon'>✉️</div>
                <h4>Verify Your Email Address</h4>
                <p>To complete your registration and secure your account, please verify your email address by clicking the button below.</p>
            </div>
        </div>
        
        <div class='action-buttons'>
            <a href='{verificationLink}' class='btn btn-primary'>Verify Email Address</a>
        </div>
        
        <div class='section'>
            <div class='security-info'>
                <h4>🔒 Security Information</h4>
                <ul>
                    <li>This verification link will expire in <strong>{tokenValidityHours} hours</strong></li>
                    <li>If you didn't create an account, you can safely ignore this email</li>
                    <li>Never share this verification link with others</li>
                </ul>
            </div>
        </div>
        
        <div class='section preview'>
            <h4>What's Next?</h4>
            <div class='feature-preview'>
                <div class='preview-item'>
                    <span class='preview-icon'>🤖</span>
                    <span>Create powerful automations</span>
                </div>
                <div class='preview-item'>
                    <span class='preview-icon'>⚡</span>
                    <span>Streamline your workflows</span>
                </div>
                <div class='preview-item'>
                    <span class='preview-icon'>📊</span>
                    <span>Monitor performance metrics</span>
                </div>
            </div>
        </div>
        
        <div class='help-section'>
            <p><strong>Having trouble?</strong> If the button doesn't work, copy and paste this link into your browser:</p>
            <p class='backup-link'>{verificationLink}</p>
        </div>
    </div>";

            return Task.FromResult(WrapInEmailTemplate("Verify Your Email", "Verify Your Email Address 📧", content));
        }

        public Task<string> GetWelcomeEmailTemplateAsync(string userName, string loginLink)
        {
            string content = $@"
    <div class='welcome-container'>
        <div class='greeting'>
            <h3>Hello {userName},</h3>
            <p class='success-message'>🎉 Your email has been verified successfully!</p>
        </div>
        
        <div class='section'>
            <h4>Get Started with OpenAutomate</h4>
            <p>Welcome to the future of automation! You're now ready to streamline your workflows and boost productivity.</p>
        </div>
        
        <div class='action-buttons'>
            <a href='{loginLink}' class='btn btn-primary'>Access Your Dashboard</a>
        </div>
        
        <div class='section'>
            <h4>Next Steps</h4>
            <div class='resource-grid'>
                <div class='resource-item'>
                    <div class='resource-icon'>🖥️</div>
                    <h5>Download OpenAutomate Agent</h5>
                    <p>Install the desktop agent to run automations on your local machine</p>
                    <a href='https://download.openautomate.io/OpenAutomate-Agent-Setup.exe' class='btn btn-secondary'>Download Agent</a>
                </div>
                
                <div class='resource-item'>
                    <div class='resource-icon'>📚</div>
                    <h5>Explore Documentation</h5>
                    <p>Learn how to create and manage automations with our comprehensive guides</p>
                    <a href='https://docs.openautomate.io/' class='btn btn-secondary'>View Docs</a>
                </div>
            </div>
        </div>
        
        <div class='section tips'>
            <h4>💡 Quick Tips</h4>
            <ul>
                <li>Start with our pre-built automation templates</li>
                <li>Connect your favorite tools and services</li>
                <li>Set up scheduled automations to run while you sleep</li>
                <li>Monitor execution logs and performance metrics</li>
            </ul>
        </div>
        
        <div class='support-section'>
            <p><strong>Need help?</strong> Our support team is here to assist you every step of the way.</p>
            <p>Contact us through the dashboard or check our documentation for answers to common questions.</p>
        </div>
    </div>";

            return Task.FromResult(WrapInEmailTemplate("Welcome to OpenAutomate", "Welcome to OpenAutomate Platform! 🚀", content));
        }

        public Task<string> GetInvitationEmailTemplateAsync(string userName, string inviterName,
    string organizationName, string invitationLink, int tokenValidityHours, bool isExistingUser)
        {
            string actionText = isExistingUser
                ? "Log in to your existing account and accept the invitation to join the team."
                : "Create your account and join the team to start collaborating on automations.";

            string buttonText = isExistingUser ? "Accept Invitation" : "Join Team";

            string content = $@"
    <div class='email-container'>
        <div class='greeting'>
            <h3>Hello {userName},</h3>
            <p>You've been invited to join an amazing automation team!</p>
        </div>
        
        <div class='section'>
            <div class='invitation-card'>
                <div class='invitation-header'>
                    <div class='invitation-icon'>🤝</div>
                    <h4>Team Invitation</h4>
                </div>
                <div class='invitation-details'>
                    <p><strong>{inviterName}</strong> has invited you to join</p>
                    <h5 class='org-name'>{organizationName}</h5>
                    <p class='invitation-description'>{actionText}</p>
                </div>
            </div>
        </div>
        
        <div class='action-buttons'>
            <a href='{invitationLink}' class='btn btn-primary'>{buttonText}</a>
        </div>
        
        <div class='section'>
            <div class='benefits-section'>
                <h4>🚀 What You'll Get</h4>
                <div class='benefits-grid'>
                    <div class='benefit-item'>
                        <span class='benefit-icon'>👥</span>
                        <span>Collaborate with your team</span>
                    </div>
                    <div class='benefit-item'>
                        <span class='benefit-icon'>🔄</span>
                        <span>Share automation workflows</span>
                    </div>
                    <div class='benefit-item'>
                        <span class='benefit-icon'>📈</span>
                        <span>Track team performance</span>
                    </div>
                    <div class='benefit-item'>
                        <span class='benefit-icon'>🛠️</span>
                        <span>Access shared resources</span>
                    </div>
                </div>
            </div>
        </div>
        
        <div class='section'>
            <div class='expiry-notice'>
                <div class='notice-icon'>⏰</div>
                <p><strong>Time Sensitive:</strong> This invitation will expire in <strong>{tokenValidityHours / 24} days</strong></p>
                <p>Don't miss out on joining the team!</p>
            </div>
        </div>
        
        <div class='help-section'>
            <p><strong>Having trouble?</strong> If the button doesn't work, copy and paste this link into your browser:</p>
            <p class='backup-link'>{invitationLink}</p>
            <p class='contact-info'>Questions? Contact {inviterName} or reach out to our support team.</p>
        </div>
    </div>";

            return Task.FromResult(WrapInEmailTemplate($"Invitation to Join {organizationName}", $"You're Invited to Join {organizationName} 🎉", content));
        }

        public Task<string> GetResetPasswordEmailTemplateAsync(string userName, string resetLink, int tokenValidityHours)
        {
            string content = $@"
    <div class='email-container'>
        <div class='greeting'>
            <h3>Hello {userName},</h3>
            <p>We received a request to reset your OpenAutomate account password.</p>
        </div>
        
        <div class='section'>
            <div class='info-box password-reset'>
                <div class='info-icon'>🔐</div>
                <h4>Password Reset Request</h4>
                <p>To create a new password for your account, click the secure button below. This will take you to a safe page where you can set your new password.</p>
            </div>
        </div>
        
        <div class='action-buttons'>
            <a href='{resetLink}' class='btn btn-primary'>Reset My Password</a>
        </div>
        
        <div class='section'>
            <div class='security-info'>
                <h4>🛡️ Security Notice</h4>
                <ul>
                    <li>This reset link will expire in <strong>{tokenValidityHours} hours</strong> for your security</li>
                    <li>If you didn't request this password reset, please ignore this email</li>
                    <li>Your current password remains unchanged until you complete the reset process</li>
                    <li>For additional security, consider enabling two-factor authentication</li>
                </ul>
            </div>
        </div>
        
        <div class='section alert-box'>
            <div class='alert-content'>
                <div class='alert-icon'>⚠️</div>
                <div class='alert-text'>
                    <h5>Didn't request this?</h5>
                    <p>If you didn't request a password reset, your account may be at risk. Please contact our support team immediately and consider changing your password.</p>
                </div>
            </div>
        </div>
        
        <div class='section tips'>
            <h4>💡 Password Security Tips</h4>
            <div class='tips-grid'>
                <div class='tip-item'>
                    <span class='tip-icon'>🔤</span>
                    <span>Use a mix of letters, numbers, and symbols</span>
                </div>
                <div class='tip-item'>
                    <span class='tip-icon'>📏</span>
                    <span>Make it at least 12 characters long</span>
                </div>
                <div class='tip-item'>
                    <span class='tip-icon'>🚫</span>
                    <span>Avoid common words or personal info</span>
                </div>
                <div class='tip-item'>
                    <span class='tip-icon'>🔄</span>
                    <span>Don't reuse passwords from other accounts</span>
                </div>
            </div>
        </div>
        
        <div class='help-section'>
            <p><strong>Having trouble?</strong> If the button doesn't work, copy and paste this link into your browser:</p>
            <p class='backup-link'>{resetLink}</p>
            <p class='contact-info'>Need additional help? Contact our support team through the OpenAutomate dashboard.</p>
        </div>
    </div>";

            return Task.FromResult(WrapInEmailTemplate("Reset Your Password", "Password Reset Request 🔐", content));
        }

        public Task<string> GetExecutionCompletionEmailTemplateAsync(string userName, string packageName, string status, DateTime startTime, DateTime? endTime, string duration, string? errorMessage = null)
        {
            var isSuccess = status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
            var statusIcon = isSuccess ? "✅" : "❌";
            var statusClass = isSuccess ? "success" : "error";
            var statusText = isSuccess ? "completed successfully" : "encountered an issue";

            // Convert UTC times to Vietnam timezone (UTC+7)
            var vietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone("Vietnam", TimeSpan.FromHours(7), "Vietnam Time", "Vietnam Time");
            var startTimeVn = TimeZoneInfo.ConvertTimeFromUtc(startTime, vietnamTimeZone);
            var endTimeVn = endTime.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(endTime.Value, vietnamTimeZone) : (DateTime?)null;

            string content = $@"
    <div class='email-container'>
        <div class='greeting'>
            <h3>Hello {userName},</h3>
            <p>Your automation execution has {statusText}.</p>
        </div>
        
        <div class='section'>
            <div class='info-box {statusClass}'>
                <div class='info-icon'>{statusIcon}</div>
                <h4>Execution {status}</h4>
                <div class='execution-details'>
                    <div class='detail-row'>
                        <span class='detail-label'>Package: </span>
                        <span class='detail-value'>{packageName}</span>
                    </div>
                    <div class='detail-row'>
                        <span class='detail-label'>Status: </span>
                        <span class='detail-value {statusClass}'>{status}</span>
                    </div>
                    <div class='detail-row'>
                        <span class='detail-label'>Started: </span>
                        <span class='detail-value'>{startTimeVn:yyyy-MM-dd HH:mm:ss} (GMT+7)</span>
                    </div>";

            if (endTime.HasValue)
            {
                content += $@"
                    <div class='detail-row'>
                        <span class='detail-label'>Finished: </span>
                        <span class='detail-value'>{endTimeVn.Value:yyyy-MM-dd HH:mm:ss} (GMT+7)</span>
                    </div>";
            }

            content += $@"
                    <div class='detail-row'>
                        <span class='detail-label'>Duration: </span>
                        <span class='detail-value'>{duration}</span>
                    </div>
                </div>";

            if (!isSuccess && !string.IsNullOrEmpty(errorMessage))
            {
                content += $@"
                <div class='error-details'>
                    <h5>Error Details:</h5>
                    <p class='error-message'>{errorMessage}</p>
                </div>";
            }

            content += @"
            </div>
        </div>";

            if (isSuccess)
            {
                content += @"
        <div class='section'>
            <h4>What's Next?</h4>
            <p>Your automation has completed successfully. You can review the execution logs and results in your dashboard.</p>
        </div>";
            }
            else
            {
                content += @"
        <div class='section'>
            <h4>Need Help?</h4>
            <p>If you need assistance troubleshooting this issue, please check the execution logs in your dashboard or contact our support team.</p>
        </div>";
            }

            content += @"
    </div>";

            var title = isSuccess ? "Execution Completed Successfully" : "Execution Failed";
            var heading = $"Automation Execution {status} {statusIcon}";

            return Task.FromResult(WrapInEmailTemplate(title, heading, content));
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
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8fafc; }
        .header { background: linear-gradient(135deg, #ea580c, #f97316); text-align: center; margin: -20px -20px 30px -20px; padding: 30px 20px; border-radius: 8px 8px 0 0; }
        .header h2 { color: white; margin: 0; font-size: 24px; font-weight: 600; }
        .footer { margin-top: 30px; padding: 20px; font-size: 12px; color: #6b7280; text-align: center; background-color: #f1f5f9; border-radius: 8px; }
        
        /* Common container styles */
        .email-container, .welcome-container { background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); }
        .greeting h3 { color: #1f2937; font-size: 20px; margin-bottom: 10px; }
        .greeting p { color: #6b7280; margin-bottom: 15px; }
        
        /* Section styles */
        .section { margin: 25px 0; }
        .section h4 { color: #374151; font-size: 18px; margin-bottom: 12px; border-bottom: 2px solid #e5e7eb; padding-bottom: 8px; }
        .section p { color: #6b7280; margin-bottom: 15px; }
        
        /* Button styles */
        .action-buttons { text-align: center; margin: 30px 0; }
        .btn { display: inline-block; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 8px; transition: all 0.2s ease; }
        .btn-primary { background-color: #ea580c; color: white; box-shadow: 0 2px 4px rgba(234, 88, 12, 0.2); }
        .btn-primary:hover { background-color: #dc2626; transform: translateY(-1px); }
        .btn-secondary { background-color: #f3f4f6; color: #374151; border: 1px solid #d1d5db; }
        .btn-secondary:hover { background-color: #e5e7eb; }
        
        /* Info boxes */
        .info-box { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; text-align: center; }
        .info-box.verification { border-left: 4px solid #3b82f6; }
        .info-box.password-reset { border-left: 4px solid #ef4444; }
        .info-box.success { border-left: 4px solid #22c55e; background-color: #f0fdf4; }
        .info-box.error { border-left: 4px solid #ef4444; background-color: #fef2f2; }
        .info-box.warning { border-left: 4px solid #f59e0b; background-color: #fffbeb; }
        .info-icon { font-size: 32px; margin-bottom: 15px; }
        .info-box h4 { color: #1f2937; margin: 10px 0; }
        .info-box p { color: #6b7280; margin-bottom: 15px; }
        
        /* Execution details */
        .execution-details { text-align: left; margin: 15px 0; }
        .detail-row { display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f3f4f6; }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { font-weight: 600; color: #374151; }
        .detail-value { color: #6b7280; }
        .detail-value.success { color: #059669; font-weight: 600; }
        .detail-value.error { color: #dc2626; font-weight: 600; }
        
        /* Error details */
        .error-details { margin-top: 20px; padding: 15px; background-color: #fef2f2; border: 1px solid #fca5a5; border-radius: 6px; text-align: left; }
        .error-details h5 { color: #dc2626; margin: 0 0 10px 0; font-size: 16px; }
        .error-message { color: #7f1d1d; margin: 0; font-family: monospace; font-size: 14px; word-break: break-word; }
        
        /* Status-specific colors */
        .status-completed { color: #059669; }
        .status-failed { color: #dc2626; }
        .status-cancelled { color: #d97706; }
        
        /* Security and help sections */
        .security-info { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; }
        .security-info h4 { color: #374151; margin-top: 0; }
        .security-info ul { color: #6b7280; padding-left: 20px; }
        .security-info li { margin-bottom: 8px; }
        
        .help-section { background-color: #f1f5f9; border-radius: 8px; padding: 20px; text-align: center; }
        .help-section p { color: #475569; margin: 8px 0; }
        .backup-link { word-break: break-all; font-family: monospace; background: #e2e8f0; padding: 8px; border-radius: 4px; font-size: 12px; }
        .contact-info { font-size: 14px; color: #6b7280; }
        
        /* Grid layouts */
        .resource-grid, .benefits-grid, .tips-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 20px; }
        .resource-item, .benefit-item, .tip-item { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; text-align: center; }
        .resource-icon, .benefit-icon, .tip-icon { font-size: 24px; margin-bottom: 10px; display: block; }
        .resource-item h5 { color: #1f2937; font-size: 16px; margin: 10px 0; }
        .resource-item p { color: #6b7280; font-size: 14px; margin-bottom: 15px; }
        
        /* Feature preview */
        .feature-preview { display: flex; flex-direction: column; gap: 10px; margin-top: 15px; }
        .preview-item { display: flex; align-items: center; gap: 12px; padding: 12px; background-color: #f8fafc; border-radius: 6px; }
        .preview-icon { font-size: 20px; }
        
        /* Success message */
        .success-message { background-color: #f0fdf4; color: #166534; padding: 12px 16px; border-radius: 6px; border-left: 4px solid #22c55e; margin: 15px 0; }
        
        /* Invitation specific */
        .invitation-card { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 25px; text-align: center; }
        .invitation-header { margin-bottom: 20px; }
        .invitation-icon { font-size: 36px; margin-bottom: 10px; }
        .org-name { color: #ea580c; font-size: 22px; margin: 15px 0; }
        .invitation-description { color: #6b7280; }
        
        .expiry-notice { display: flex; align-items: center; gap: 15px; background-color: #fef3c7; border: 1px solid #fbbf24; border-radius: 8px; padding: 15px; }
        .notice-icon { font-size: 24px; }
        
        /* Alert box */
        .alert-box { background-color: #fef2f2; border: 1px solid #fca5a5; border-radius: 8px; padding: 20px; }
        .alert-content { display: flex; gap: 15px; align-items: flex-start; }
        .alert-icon { font-size: 24px; margin-top: 2px; }
        .alert-text h5 { color: #dc2626; margin: 0 0 8px 0; }
        .alert-text p { color: #7f1d1d; margin: 0; }
        
        /* Tips section */
        .tips { background-color: #fefce8; border: 1px solid #fde047; border-radius: 8px; padding: 20px; }
        .tips h4 { color: #a16207; margin-top: 0; }
        .tips ul { color: #713f12; padding-left: 20px; }
        .tips li { margin-bottom: 8px; }
        
        /* Preview section */
        .preview { background-color: #f0f9ff; border: 1px solid #7dd3fc; border-radius: 8px; padding: 20px; }
        .preview h4 { color: #0c4a6e; margin-top: 0; }
        
        /* Benefits section */
        .benefits-section { background-color: #f0fdf4; border: 1px solid #86efac; border-radius: 8px; padding: 20px; }
        .benefits-section h4 { color: #166534; margin-top: 0; }
        
        /* Support section */
        .support-section { background-color: #f1f5f9; border-radius: 8px; padding: 20px; text-align: center; }
        .support-section p { color: #475569; margin: 8px 0; }
        
        /* Responsive design */
        @media (max-width: 500px) {
            .resource-grid, .benefits-grid, .tips-grid { grid-template-columns: 1fr; }
            .btn { display: block; margin: 10px 0; }
            .alert-content { flex-direction: column; text-align: center; }
            .expiry-notice { flex-direction: column; text-align: center; }
        }
        
        a { color: #ea580c; }";
        }

        #endregion
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously 