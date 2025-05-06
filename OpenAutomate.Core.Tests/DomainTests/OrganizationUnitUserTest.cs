using Xunit;
using OpenAutomate.Core.Domain.Entities;
using System;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class OrganizationUnitUserTest
    {
        [Fact]
        public void OrganizationUnitUser_WhenCreated_HasExpectedDefaults()
        {
            // Arrange & Act
            var orgUnitUser = new OrganizationUnitUser();

            // Assert
            Assert.NotNull(orgUnitUser);
            Assert.Equal(Guid.Empty, orgUnitUser.UserId);
            Assert.Null(orgUnitUser.User);
        }
        [Fact]
        public void OrganizationUnitUser_SetUserId_UserIdIsSet()
        {
            // Arrange
            var orgUnitUser = new OrganizationUnitUser();
            var userId = Guid.NewGuid();

            // Act
            orgUnitUser.UserId = userId;

            // Assert
            Assert.Equal(userId, orgUnitUser.UserId);
        }
        [Fact]
        public void OrganizationUnitUser_LinkUser_UserIsLinked()
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };
            var orgUnitUser = new OrganizationUnitUser { User = user };

            // Act
            var linkedUser = orgUnitUser.User;

            // Assert
            Assert.NotNull(linkedUser);
            Assert.Equal("John", linkedUser.FirstName);
            Assert.Equal("Doe", linkedUser.LastName);
        }

    }
}
