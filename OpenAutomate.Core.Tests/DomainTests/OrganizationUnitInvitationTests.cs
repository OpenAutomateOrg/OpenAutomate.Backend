using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Entities;
using Xunit;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class OrganizationUnitInvitationTests
    {
        [Fact]
        public void Can_Create_Valid_OrganizationUnitInvitation()
        {
            var invitation = new OrganizationUnitInvitation
            {
                OrganizationUnitId = Guid.NewGuid(),
                RecipientEmail = "test@example.com",
                InviterId = Guid.NewGuid(),
                Token = "token123",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = InvitationStatus.Pending
            };

            Assert.Equal("test@example.com", invitation.RecipientEmail);
            Assert.Equal("token123", invitation.Token);
            Assert.Equal(InvitationStatus.Pending, invitation.Status);
        }

        [Fact]
        public void Validation_Fails_If_Required_Fields_Missing()
        {
            var invitation = new OrganizationUnitInvitation
            {
                RecipientEmail = string.Empty, // Required field initialized
                Token = string.Empty           // Required field initialized
            };
            var context = new ValidationContext(invitation);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(invitation, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("RecipientEmail"));
            Assert.Contains(results, r => r.MemberNames.Contains("Token"));
        }


        [Fact]
        public void Can_Set_And_Get_Status()
        {
            var invitation = new OrganizationUnitInvitation
            {
                OrganizationUnitId = Guid.NewGuid(),
                RecipientEmail = "test@example.com",
                Token = "token123",
                InviterId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = InvitationStatus.Pending
            };
            invitation.Status = InvitationStatus.Accepted;
            Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        }

        [Theory]
        [InlineData(InvitationStatus.Pending)]
        [InlineData(InvitationStatus.Accepted)]
        [InlineData(InvitationStatus.Expired)]
        [InlineData(InvitationStatus.Revoked)]
        public void Can_Set_All_InvitationStatus_Values(InvitationStatus status)
        {
            var invitation = new OrganizationUnitInvitation
            {
                OrganizationUnitId = Guid.NewGuid(),
                RecipientEmail = "test@example.com",
                Token = "token123",
                InviterId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = status
            };
            Assert.Equal(status, invitation.Status);
        }
    }
} 