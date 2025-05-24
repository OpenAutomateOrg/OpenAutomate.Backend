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
    public class InvitationService : IInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IConfiguration configuration,
            ILogger<InvitationService> logger)
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
                _logger.LogInformation("Sending invitation to {Email} for organization {OrganizationId}", email, organizationId);
                
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
                
                // Kiểm tra xem email đã tồn tại trong hệ thống chưa
                var existingUser = await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email == email);
                bool isExistingUser = existingUser != null;
                
                // Nếu là người dùng đã tồn tại, kiểm tra xem họ đã tham gia tổ chức này chưa
                if (isExistingUser)
                {
                    var alreadyMember = await _unitOfWork.OrganizationUnitUsers.GetFirstOrDefaultAsync(
                        ou => ou.UserId == existingUser.Id && ou.OrganizationUnitId == organizationId);
                        
                    if (alreadyMember != null)
                    {
                        _logger.LogWarning("User {Email} is already a member of organization {OrganizationId}", email, organizationId);
                        throw new ServiceException("Người dùng đã là thành viên của tổ chức này");
                    }
                }
                
                // Xóa các token mời cũ cho email và tổ chức này
                var existingTokens = await _unitOfWork.InvitationTokens.GetAllAsync(
                    t => t.Email == email && t.OrganizationUnitId == organizationId && !t.IsUsed);
                    
                foreach (var token in existingTokens)
                {
                    _unitOfWork.InvitationTokens.Remove(token);
                }
                await _unitOfWork.CompleteAsync();
                
                // Tạo token mới
                string tokenString = GenerateToken();
                
                // Tạo entity token
                var invitationToken = new InvitationToken
                {
                    OrganizationUnitId = organizationId,
                    InviterId = inviterId,
                    Email = email,
                    Name = name,
                    Token = tokenString,
                    ExpiresAt = DateTime.UtcNow.AddDays(7), // Hết hạn sau 7 ngày
                    IsUsed = false
                };
                
                await _unitOfWork.InvitationTokens.AddAsync(invitationToken);
                await _unitOfWork.CompleteAsync();
                
                // Tạo link mời
                var baseUrl = _configuration["FrontendUrl"];
                var invitationLink = $"{baseUrl}/invitation?token={tokenString}";
                
                // Lấy template email
                var emailContent = await _emailTemplateService.GetInvitationEmailTemplateAsync(
                    name,
                    $"{inviter.FirstName ?? ""} {inviter.LastName ?? ""}",
                    organization.Name ?? "Organization",
                    invitationLink,
                    168, // 7 days = 168 hours
                    isExistingUser);
                
                // Gửi email
                string subject = $"Lời mời tham gia tổ chức {organization.Name ?? "Organization"} trên OpenAutomate";
                await _emailService.SendEmailAsync(email, subject, emailContent);
                
                _logger.LogInformation("Invitation email sent to {Email} for organization {OrganizationId}", email, organizationId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation to {Email} for organization {OrganizationId}", email, organizationId);
                throw;
            }
        }
        
        public async Task<InvitationToken> ValidateInvitationTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;
                    
                // Tìm token trong database
                var invitationToken = await _unitOfWork.InvitationTokens.GetFirstOrDefaultAsync(
                    t => t.Token == token,
                    t => t.OrganizationUnit,
                    t => t.Inviter);
                    
                // Kiểm tra token có tồn tại không
                if (invitationToken == null)
                    return null;
                    
                // Kiểm tra token đã được sử dụng chưa
                if (invitationToken.IsUsed)
                    return null;
                    
                // Kiểm tra token có hết hạn không
                if (invitationToken.IsExpired)
                    return null;
                    
                return invitationToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token: {Message}", ex.Message);
                return null;
            }
        }
        
        public async Task<bool> AcceptInvitationAsync(string token, Guid userId)
        {
            try
            {
                // Xác thực token
                var invitationToken = await ValidateInvitationTokenAsync(token);
                if (invitationToken == null)
                {
                    _logger.LogWarning("Invalid invitation token: {Token}", token);
                    throw new ServiceException("Token không hợp lệ hoặc đã hết hạn");
                }
                
                // Kiểm tra người dùng
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    throw new ServiceException("Người dùng không tồn tại");
                }
                
                // Kiểm tra xem người dùng đã tham gia tổ chức này chưa
                var existingMembership = await _unitOfWork.OrganizationUnitUsers.GetFirstOrDefaultAsync(
                    ou => ou.UserId == userId && ou.OrganizationUnitId == invitationToken.OrganizationUnitId);
                    
                if (existingMembership != null)
                {
                    _logger.LogWarning("User {UserId} is already a member of organization {OrganizationId}", 
                        userId, invitationToken.OrganizationUnitId);
                    throw new ServiceException("Bạn đã là thành viên của tổ chức này");
                }
                
                // Thêm người dùng vào tổ chức
                var organizationUnitUser = new OrganizationUnitUser
                {
                    UserId = userId,
                    OrganizationUnitId = invitationToken.OrganizationUnitId
                };
                
                await _unitOfWork.OrganizationUnitUsers.AddAsync(organizationUnitUser);
                
                // Tìm authority USER trong tổ chức
                var userAuthority = await _unitOfWork.Authorities.GetFirstOrDefaultAsync(
                    a => a.Name == "USER" && a.OrganizationUnitId == invitationToken.OrganizationUnitId);
                    
                if (userAuthority != null)
                {
                    // Thêm quyền USER cho người dùng trong tổ chức
                    var userAuthorityAssignment = new UserAuthority
                    {
                        UserId = userId,
                        AuthorityId = userAuthority.Id,
                        OrganizationUnitId = invitationToken.OrganizationUnitId
                    };
                    
                    await _unitOfWork.UserAuthorities.AddAsync(userAuthorityAssignment);
                }
                
                // Cập nhật token đã sử dụng
                invitationToken.IsUsed = true;
                invitationToken.UsedAt = DateTime.UtcNow;
                invitationToken.AcceptedByUserId = userId;
                
                _unitOfWork.InvitationTokens.Update(invitationToken);
                await _unitOfWork.CompleteAsync();
                
                _logger.LogInformation("User {UserId} accepted invitation to join organization {OrganizationId}", 
                    userId, invitationToken.OrganizationUnitId);
                
                return true;
            }
            catch (ServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation: {Message}", ex.Message);
                throw new ServiceException("Có lỗi xảy ra khi chấp nhận lời mời");
            }
        }
        
        private string GenerateToken()
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