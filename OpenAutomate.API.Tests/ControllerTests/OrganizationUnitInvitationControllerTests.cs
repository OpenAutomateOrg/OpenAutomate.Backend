using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.OrganizationUnitInvitation;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class OrganizationUnitInvitationControllerTests
    {
        private readonly Mock<IOrganizationUnitInvitationService> _mockInvitationService;
        private readonly Mock<IOrganizationUnitService> _mockOrgService;
        private readonly OrganizationUnitInvitationController _controller;
        private readonly User _testUser;
        private readonly Guid _orgId;

        public OrganizationUnitInvitationControllerTests()
        {
            _mockInvitationService = new Mock<IOrganizationUnitInvitationService>();
            _mockOrgService = new Mock<IOrganizationUnitService>();
            _controller = new OrganizationUnitInvitationController(_mockInvitationService.Object, _mockOrgService.Object);

            _testUser = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Items["User"] = _testUser;
            _orgId = Guid.NewGuid();
        }

        #region InviteUser

        [Fact]
        public async Task InviteUser_OrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync((OrganizationUnitResponseDto)null);

            // Act
            var result = await _controller.InviteUser("tenant", new InviteUserRequest { Email = "test@example.com" });

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task InviteUser_Success_ReturnsOk()
        {
            // Arrange
            var org = new OrganizationUnitResponseDto { Id = _orgId, Name = "Org", Slug = "tenant" };
            var inviteRequest = new InviteUserRequest { Email = "test@example.com" };
            var inviteResult = new OrganizationUnitInvitationDto { Id = Guid.NewGuid(), RecipientEmail = inviteRequest.Email, OrganizationUnitId = _orgId };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync(org);
            _mockInvitationService.Setup(s => s.InviteUserAsync(_orgId, inviteRequest, _testUser.Id)).ReturnsAsync(inviteResult);

            // Act
            var result = await _controller.InviteUser("tenant", inviteRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(inviteResult, okResult.Value);
        }

        [Fact]
        public async Task InviteUser_AlreadyMemberOrPending_ReturnsBadRequest()
        {
            // Arrange
            var org = new OrganizationUnitResponseDto { Id = _orgId, Name = "Org", Slug = "tenant" };
            var inviteRequest = new InviteUserRequest { Email = "test@example.com" };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync(org);
            _mockInvitationService.Setup(s => s.InviteUserAsync(_orgId, inviteRequest, _testUser.Id))
                .ThrowsAsync(new Exception("already a member of this organization"));

            // Act
            var result = await _controller.InviteUser("tenant", inviteRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("already a member", badRequest.Value.ToString());
        }

        #endregion

        #region AcceptInvitation

        [Fact]
        public async Task AcceptInvitation_InvitationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockInvitationService.Setup(s => s.GetInvitationByTokenAsync("token")).ReturnsAsync((OrganizationUnitInvitation)null);

            // Act
            var result = await _controller.AcceptInvitation(new AcceptInvitationRequest { Token = "token" });

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Invitation not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task AcceptInvitation_Success_ReturnsOk()
        {
            // Arrange
            var invitation = new OrganizationUnitInvitation { RecipientEmail = "test@example.com", Token = "token", Status = InvitationStatus.Pending };
            _mockInvitationService.Setup(s => s.GetInvitationByTokenAsync("token")).ReturnsAsync(invitation);
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync("token", _testUser.Id)).ReturnsAsync(AcceptInvitationResult.Success);

            // Act
            var result = await _controller.AcceptInvitation(new AcceptInvitationRequest { Token = "token" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("success", ok.Value.ToString());
        }

        [Fact]
        public async Task AcceptInvitation_Expired_ReturnsGone()
        {
            // Arrange
            var invitation = new OrganizationUnitInvitation { RecipientEmail = "test@example.com", Token = "token", Status = InvitationStatus.Pending };
            _mockInvitationService.Setup(s => s.GetInvitationByTokenAsync("token")).ReturnsAsync(invitation);
            _mockInvitationService.Setup(s => s.AcceptInvitationAsync("token", _testUser.Id)).ReturnsAsync(AcceptInvitationResult.InvitationExpired);

            // Act
            var result = await _controller.AcceptInvitation(new AcceptInvitationRequest { Token = "token" });

            // Assert
            var gone = Assert.IsType<ObjectResult>(result);
            Assert.Equal(410, gone.StatusCode);
        }

        #endregion

        #region CheckInvitation

        [Fact]
        public async Task CheckInvitation_OrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync((OrganizationUnitResponseDto)null);

            // Act
            var result = await _controller.CheckInvitation("tenant", "test@example.com");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CheckInvitation_Pending_ReturnsOkWithInvitedTrue()
        {
            // Arrange
            var org = new OrganizationUnitResponseDto { Id = _orgId, Name = "Org", Slug = "tenant" };
            var invitation = new OrganizationUnitInvitation { RecipientEmail = "test@example.com", Status = InvitationStatus.Pending, Token = "token" };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync(org);
            _mockInvitationService.Setup(s => s.GetPendingInvitationAsync(_orgId, "test@example.com")).ReturnsAsync(invitation);

            // Act
            var result = await _controller.CheckInvitation("tenant", "test@example.com");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("invited", ok.Value.ToString());
        }

        [Fact]
        public async Task CheckInvitation_NotPending_ReturnsOkWithInvitedFalse()
        {
            // Arrange
            var org = new OrganizationUnitResponseDto { Id = _orgId, Name = "Org", Slug = "tenant" };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync(org);
            _mockInvitationService.Setup(s => s.GetPendingInvitationAsync(_orgId, "test@example.com")).ReturnsAsync((OrganizationUnitInvitation)null);

            // Act
            var result = await _controller.CheckInvitation("tenant", "test@example.com");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("invited", ok.Value.ToString());
        }

        #endregion

        #region CheckInvitationToken

        [Fact]
        public async Task CheckInvitationToken_TokenMissing_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CheckInvitationToken(null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Token is required", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CheckInvitationToken_InvitationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockInvitationService.Setup(s => s.GetInvitationByTokenAsync("token")).ReturnsAsync((OrganizationUnitInvitation)null);

            // Act
            var result = await _controller.CheckInvitationToken("token");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Invitation not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task CheckInvitationToken_Valid_ReturnsOk()
        {
            // Arrange
            var invitation = new OrganizationUnitInvitation { RecipientEmail = "test@example.com", Token = "token", Status = InvitationStatus.Pending, ExpiresAt = DateTime.UtcNow, InviterId = Guid.NewGuid() };
            _mockInvitationService.Setup(s => s.GetInvitationByTokenAsync("token")).ReturnsAsync(invitation);

            // Act
            var result = await _controller.CheckInvitationToken("token");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("recipientEmail", ok.Value.ToString());
        }

        #endregion

        #region ListInvitations

        [Fact]
        public async Task ListInvitations_OrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync((OrganizationUnitResponseDto)null);

            // Act
            var result = await _controller.ListInvitations("tenant");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ListInvitations_Success_ReturnsOk()
        {
            // Arrange
            var org = new OrganizationUnitResponseDto { Id = _orgId, Name = "Org", Slug = "tenant" };
            var invitations = new List<OrganizationUnitInvitationDto> {
                new OrganizationUnitInvitationDto { Id = Guid.NewGuid(), RecipientEmail = "test@example.com", OrganizationUnitId = _orgId }
            };
            _mockOrgService.Setup(s => s.GetOrganizationUnitBySlugAsync("tenant")).ReturnsAsync(org);
            _mockInvitationService.Setup(s => s.ListInvitationsByOrganizationUnitAsync(_orgId)).ReturnsAsync(invitations);

            // Act
            var result = await _controller.ListInvitations("tenant");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Invitations", ok.Value.ToString());
        }

        #endregion
    }
}