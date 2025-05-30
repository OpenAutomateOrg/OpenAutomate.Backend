using Microsoft.Extensions.Configuration;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.OrganizationInvitation;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Infrastructure.Services
{
    public class OrganizationInvitationService : IOrganizationInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public OrganizationInvitationService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<OrganizationInvitationDto> InviteUserAsync(Guid organizationId, InviteUserRequest request, Guid inviterId)
        {
            var organization = await _unitOfWork.OrganizationUnits.GetByIdAsync(organizationId);
            if (organization == null)
                throw new Exception("Organization not found");

            var existingInvitation = await _unitOfWork.OrganizationInvitations
                .GetFirstOrDefaultAsync(i => i.OrganizationUnitId == organizationId
                                          && i.RecipientEmail == request.Email
                                          && i.Status == InvitationStatus.Pending);
            if (existingInvitation != null)
                throw new Exception("There is already a pending invitation for this email");

            var invitation = new OrganizationInvitation
            {
                OrganizationUnitId = organizationId,
                RecipientEmail = request.Email,
                InviterId = inviterId,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Token = Guid.NewGuid().ToString()
            };

            await _unitOfWork.OrganizationInvitations.AddAsync(invitation);
            await _unitOfWork.CompleteAsync();

            // Check if email is already registered
            var isExistingUser = await _unitOfWork.Users.AnyAsync(u => u.Email == invitation.RecipientEmail);

            // Send an invitation email
            await _notificationService.SendOrganizationInvitationAsync(
                inviterId,
                invitation.RecipientEmail,
                organizationId,
                invitation.Token,
                invitation.ExpiresAt,
                isExistingUser
            );

            return new OrganizationInvitationDto
            {
                Id = invitation.Id,
                RecipientEmail = invitation.RecipientEmail,
                Status = invitation.Status.ToString(),
                ExpiresAt = invitation.ExpiresAt,
                InviterId = invitation.InviterId,
                OrganizationUnitId = invitation.OrganizationUnitId
            };
        }

        public async Task<AcceptInvitationResult> AcceptInvitationAsync(string token, Guid userId)
        {
            var invitation = await _unitOfWork.OrganizationInvitations.GetFirstOrDefaultAsync(i => i.Token == token);
            if (invitation == null || invitation.Status != InvitationStatus.Pending)
                return AcceptInvitationResult.InvitationNotFoundOrInvalid;

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                _unitOfWork.OrganizationInvitations.Update(invitation);
                await _unitOfWork.CompleteAsync();
                return AcceptInvitationResult.InvitationExpired;
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return AcceptInvitationResult.UserNotFound;

            if (!string.Equals(user.Email, invitation.RecipientEmail, StringComparison.OrdinalIgnoreCase))
                return AcceptInvitationResult.NotInvited;

            var orgUser = await _unitOfWork.OrganizationUnitUsers
                .GetFirstOrDefaultAsync(ou => ou.OrganizationUnitId == invitation.OrganizationUnitId && ou.UserId == userId);
            if (orgUser == null)
            {
                await _unitOfWork.OrganizationUnitUsers.AddAsync(new OrganizationUnitUser
                {
                    OrganizationUnitId = invitation.OrganizationUnitId,
                    UserId = userId
                });
            }

            var userRole = await _unitOfWork.Authorities
                .GetFirstOrDefaultAsync(a => a.OrganizationUnitId == invitation.OrganizationUnitId && a.Name == "USER");
            if (userRole != null)
            {
                var userAuthority = await _unitOfWork.UserAuthorities
                    .GetFirstOrDefaultAsync(ua => ua.UserId == userId && ua.AuthorityId == userRole.Id);
                if (userAuthority == null)
                {
                    await _unitOfWork.UserAuthorities.AddAsync(new UserAuthority
                    {
                        UserId = userId,
                        AuthorityId = userRole.Id,
                        OrganizationUnitId = invitation.OrganizationUnitId
                    });
                }
            }

            invitation.Status = InvitationStatus.Accepted;
            _unitOfWork.OrganizationInvitations.Update(invitation);

            await _unitOfWork.CompleteAsync();
            return AcceptInvitationResult.Success;
        }

        public async Task<OrganizationInvitation?> GetPendingInvitationAsync(Guid organizationId, string email)
        {
            return await _unitOfWork.OrganizationInvitations
                .GetFirstOrDefaultAsync(i => i.OrganizationUnitId == organizationId
                                          && i.RecipientEmail == email
                                          && i.Status == InvitationStatus.Pending);
        }

        public async Task<OrganizationInvitation> GetInvitationByTokenAsync(string token)
        {
            return await _unitOfWork.OrganizationInvitations.GetFirstOrDefaultAsync(i => i.Token == token);
        }
    }
}
