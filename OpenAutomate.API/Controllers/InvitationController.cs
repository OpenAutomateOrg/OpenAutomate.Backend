using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenAutomate.API.Extensions;
using OpenAutomate.Core.Dto.InvitationDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.API.Controllers
{
    [ApiController]
    [Route("api/invitation")]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;
        private readonly ILogger<InvitationController> _logger;

        public InvitationController(
            IInvitationService invitationService,
            ILogger<InvitationController> logger)
        {
            _invitationService = invitationService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi lời mời tham gia tổ chức
        /// </summary>
        /// <param name="request">Thông tin lời mời</param>
        /// <returns>Kết quả thao tác</returns>
        [HttpPost("send")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationRequestDto request)
        {
            try
            {
                // Lấy ID người dùng hiện tại từ claims
                var inviterId = User.GetUserId();
                if (inviterId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                await _invitationService.SendInvitationAsync(
                    inviterId,
                    request.OrganizationId,
                    request.Email,
                    request.Name ?? request.Email);

                return Ok(new { message = "Đã gửi lời mời thành công" });
            }
            catch (ServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation: {Message}", ex.Message);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi lời mời" });
            }
        }

        /// <summary>
        /// Xác thực token mời
        /// </summary>
        /// <param name="token">Token cần xác thực</param>
        /// <returns>Thông tin lời mời nếu token hợp lệ</returns>
        [HttpGet("validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ValidateInvitationResponseDto>> ValidateInvitation([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Token không được để trống" });
                }

                var invitationToken = await _invitationService.ValidateInvitationTokenAsync(token);
                
                var response = new ValidateInvitationResponseDto
                {
                    IsValid = invitationToken != null,
                    Email = invitationToken?.Email,
                    Name = invitationToken?.Name,
                    OrganizationId = invitationToken?.OrganizationUnitId,
                    OrganizationName = invitationToken?.OrganizationUnit?.Name,
                    InviterName = invitationToken?.Inviter != null
                        ? $"{invitationToken.Inviter.FirstName ?? ""} {invitationToken.Inviter.LastName ?? ""}"
                        : "",
                    ExpiresAt = invitationToken?.ExpiresAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token: {Message}", ex.Message);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xác thực token" });
            }
        }

        /// <summary>
        /// Chấp nhận lời mời tham gia tổ chức
        /// </summary>
        /// <param name="request">Request chứa token lời mời</param>
        /// <returns>Kết quả thao tác</returns>
        [HttpPost("accept")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequestDto request)
        {
            try
            {
                // Lấy ID người dùng hiện tại từ claims
                var userId = User.GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng" });
                }

                await _invitationService.AcceptInvitationAsync(request.Token, userId);

                return Ok(new { message = "Đã chấp nhận lời mời thành công" });
            }
            catch (ServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation: {Message}", ex.Message);
                return StatusCode(500, new { message = "Có lỗi xảy ra khi chấp nhận lời mời" });
            }
        }
    }
} 