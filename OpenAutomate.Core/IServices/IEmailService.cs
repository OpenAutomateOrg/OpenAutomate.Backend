namespace OpenAutomate.Core.IServices
{
    /// <summary>
    /// Interface for email service functionality
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to a single recipient
        /// </summary>
        /// <param name="recipient">Email address of the recipient</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body is HTML formatted</param>
        Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true);
        
        /// <summary>
        /// Sends an email to multiple recipients
        /// </summary>
        /// <param name="recipients">Email addresses of recipients</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body is HTML formatted</param>
        Task SendEmailToMultipleRecipientsAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true);
    }
} 