using OpenAutomate.Infrastructure.Services;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Services
{
    public class EmailTemplateServiceTests
    {
        [Fact]
        public async Task VerificationTemplate_IncludesUserNameAndLink()
        {
            var service = new EmailTemplateService();
            string html = await service.GetVerificationEmailTemplateAsync("John", "https://example.com", 24);
            Assert.Contains("John", html);
            Assert.Contains("https://example.com", html);
        }

        [Fact]
        public async Task WelcomeTemplate_IncludesLoginLink()
        {
            var service = new EmailTemplateService();
            string html = await service.GetWelcomeEmailTemplateAsync("Jane", "https://example.com/login");
            Assert.Contains("Jane", html);
            Assert.Contains("https://example.com/login", html);
        }

        [Fact]
        public async Task InvitationTemplate_IncludesOrganizationName()
        {
            var service = new EmailTemplateService();
            string html = await service.GetInvitationEmailTemplateAsync("Bob", "Alice", "Org", "https://example.com/invite", 48, true);
            Assert.Contains("Org", html);
            Assert.Contains("https://example.com/invite", html);
        }

        [Fact]
        public async Task ResetPasswordTemplate_IncludesResetLink()
        {
            var service = new EmailTemplateService();
            string html = await service.GetResetPasswordEmailTemplateAsync("Sam", "https://example.com/reset", 4);
            Assert.Contains("https://example.com/reset", html);
            Assert.Contains("Sam", html);
        }
    }
}
