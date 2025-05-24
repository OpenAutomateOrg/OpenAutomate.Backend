using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class SimpleInvitationService : IInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleInvitationService> _logger;

        public SimpleInvitationService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IConfiguration configuration,
            ILogger<SimpleInvitationService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendInvitationAsync(Guid inviterId, Guid organizationId, string email, string name)
        {
            try
            {
                _logger.LogInformation("SimpleInvitationService: Sending invitation to {Email} for organization {OrganizationId}", 
                    email, organizationId);
                
                // Kiểm tra người mời
                var inviter = await _unitOfWork.Users.GetByIdAsync(inviterId);
                if (inviter == null)
                {
                    _logger.LogWarning("Inviter with ID {InviterId} not found", inviterId);
                    throw new ServiceException("Người mời không tồn tại");
                }
                
                // Kiểm tra tổ chức
                var organization = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationId);
                if (organization == null)
                {
                    _logger.LogWarning("Organization with ID {OrganizationId} not found", organizationId);
                    throw new ServiceException("Tổ chức không tồn tại");
                }
                
                // Tạo token đơn giản (không lưu vào DB trong phiên bản đơn giản này)
                string token = GenerateSimpleToken();
                
                // Tạo link mời
                var baseUrl = _configuration["FrontendUrl"];
                var invitationLink = $"{baseUrl}/invitation?token={token}";
                
                // Get email template
                var emailContent = await _emailTemplateService.GetInvitationEmailTemplateAsync(
                    name,
                    $"{inviter.FirstName ?? ""} {inviter.LastName ?? ""}",
                    organization.Name ?? "Organization",
                    invitationLink,
                    168, // 7 days = 168 hours
                    false);
                
                // Gửi email
                string subject = $"Lời mời tham gia tổ chức {organization.Name ?? "Organization"} trên OpenAutomate";
                await _emailService.SendEmailAsync(email, subject, emailContent);
                
                _logger.LogInformation("SimpleInvitationService: Invitation email sent successfully to {Email}", email);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SimpleInvitationService: Failed to send invitation to {Email}: {Message}", 
                    email, ex.Message);
                throw;
            }
        }
        
        // Phương thức này chỉ là giả định trong phiên bản đơn giản
        public async Task<InvitationToken> ValidateInvitationTokenAsync(string token)
        {
            // Trong phiên bản đơn giản, chúng ta luôn trả về null
            _logger.LogInformation("SimpleInvitationService: ValidateInvitationTokenAsync called with token {Token}", token);
            return null;
        }
        
        // Phương thức này chỉ là giả định trong phiên bản đơn giản
        public async Task<bool> AcceptInvitationAsync(string token, Guid userId)
        {
            // Trong phiên bản đơn giản, chúng ta luôn trả về true
            _logger.LogInformation("SimpleInvitationService: AcceptInvitationAsync called with token {Token}", token);
            return true;
        }
        
        private string GenerateSimpleToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[32];
            rng.GetBytes(randomBytes);
            
            return Convert.ToBase64String(randomBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
        }
    }
} 