using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.IServices
{
    public interface IInvitationService
    {
        /// <summary>
        /// Tạo và gửi lời mời tham gia tổ chức
        /// </summary>
        /// <param name="inviterId">ID của người mời</param>
        /// <param name="organizationId">ID của tổ chức</param>
        /// <param name="email">Email của người được mời</param>
        /// <param name="name">Tên của người được mời</param>
        /// <returns>Kết quả thao tác</returns>
        Task<bool> SendInvitationAsync(Guid inviterId, Guid organizationId, string email, string name);
        
        /// <summary>
        /// Xác thực token mời
        /// </summary>
        /// <param name="token">Token cần xác thực</param>
        /// <returns>Thông tin token nếu hợp lệ, null nếu không hợp lệ</returns>
        Task<Domain.Entities.InvitationToken> ValidateInvitationTokenAsync(string token);
        
        /// <summary>
        /// Chấp nhận lời mời tham gia tổ chức
        /// </summary>
        /// <param name="token">Token lời mời</param>
        /// <param name="userId">ID của người dùng chấp nhận lời mời</param>
        /// <returns>Kết quả thao tác</returns>
        Task<bool> AcceptInvitationAsync(string token, Guid userId);
    }
} 