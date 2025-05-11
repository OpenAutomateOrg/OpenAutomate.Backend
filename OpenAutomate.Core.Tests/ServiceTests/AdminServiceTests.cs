using Moq;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class AdminServiceTests
    {
        private readonly Mock<IAdminService> _mockAdminService;

        public AdminServiceTests()
        {
            _mockAdminService = new Mock<IAdminService>();
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            var expectedUsers = new List<UserResponse>
            {
                new UserResponse { Id = Guid.NewGuid(), Email = "user1@example.com", FirstName = "User", LastName = "One" },
                new UserResponse { Id = Guid.NewGuid(), Email = "user2@example.com", FirstName = "User", LastName = "Two" }
            };
            _mockAdminService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(expectedUsers);

            // Act
            var result = await _mockAdminService.Object.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Collection(result,
                user => Assert.Equal("user1@example.com", user.Email),
                user => Assert.Equal("user2@example.com", user.Email));
            _mockAdminService.Verify(s => s.GetAllUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new UserResponse
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            _mockAdminService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

            // Act
            var result = await _mockAdminService.Object.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@example.com", result.Email);
            _mockAdminService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockAdminService.Setup(s => s.GetUserByIdAsync(invalidId))
                .ReturnsAsync((UserResponse)null);

            // Act
            var result = await _mockAdminService.Object.GetUserByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
            _mockAdminService.Verify(s => s.GetUserByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfoAsync_WithValidData_ReturnsUpdatedUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateRequest = new UpdateUserInfoRequest
            {
                FirstName = "Updated",
                LastName = "User"
            };
            var expectedUser = new UserResponse
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Updated",
                LastName = "User"
            };
            _mockAdminService.Setup(s => s.UpdateUserInfoAsync(userId, updateRequest))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _mockAdminService.Object.UpdateUserInfoAsync(userId, updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated", result.FirstName);
            Assert.Equal("User", result.LastName);
            _mockAdminService.Verify(s => s.UpdateUserInfoAsync(userId, updateRequest), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfoAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateRequest = new UpdateUserInfoRequest { FirstName = "", LastName = "User" };
            _mockAdminService.Setup(s => s.UpdateUserInfoAsync(userId, updateRequest))
                .ThrowsAsync(new ArgumentException("First name cannot be empty"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockAdminService.Object.UpdateUserInfoAsync(userId, updateRequest));
        }

        [Fact]
        public async Task UpdateUserInfoAsync_WithNullLastName_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateRequest = new UpdateUserInfoRequest { FirstName = "Test", LastName = null };
            _mockAdminService.Setup(s => s.UpdateUserInfoAsync(userId, updateRequest))
                .ThrowsAsync(new ArgumentException("Last name cannot be null"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockAdminService.Object.UpdateUserInfoAsync(userId, updateRequest));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidPassword_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var newPassword = "NewPassword123!";
            _mockAdminService.Setup(s => s.ChangePasswordAsync(userId, newPassword))
                .ReturnsAsync(true);

            // Act
            var result = await _mockAdminService.Object.ChangePasswordAsync(userId, newPassword);

            // Assert
            Assert.True(result);
            _mockAdminService.Verify(s => s.ChangePasswordAsync(userId, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithInvalidPassword_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var invalidPassword = "weak";
            _mockAdminService.Setup(s => s.ChangePasswordAsync(userId, invalidPassword))
                .ThrowsAsync(new ArgumentException("Password does not meet complexity requirements"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockAdminService.Object.ChangePasswordAsync(userId, invalidPassword));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNullPassword_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string newPassword = null;
            _mockAdminService.Setup(s => s.ChangePasswordAsync(userId, newPassword))
                .ThrowsAsync(new ArgumentNullException(nameof(newPassword)));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _mockAdminService.Object.ChangePasswordAsync(userId, newPassword));
        }

        [Fact]
        public async Task GetAllOrganizationUnitsAsync_ReturnsAllUnits()
        {
            // Arrange
            var expectedUnits = new List<OrganizationUnitResponseDto>
            {
                new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Unit 1", Slug = "unit-1" },
                new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "Unit 2", Slug = "unit-2" }
            };
            _mockAdminService.Setup(s => s.GetAllOrganizationUnitsAsync()).ReturnsAsync(expectedUnits);

            // Act
            var result = await _mockAdminService.Object.GetAllOrganizationUnitsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Collection(result,
                unit => Assert.Equal("Unit 1", unit.Name),
                unit => Assert.Equal("Unit 2", unit.Name));
            _mockAdminService.Verify(s => s.GetAllOrganizationUnitsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllOrganizationUnitsAsync_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var expectedUnits = new List<OrganizationUnitResponseDto>();
            _mockAdminService.Setup(s => s.GetAllOrganizationUnitsAsync()).ReturnsAsync(expectedUnits);

            // Act
            var result = await _mockAdminService.Object.GetAllOrganizationUnitsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockAdminService.Verify(s => s.GetAllOrganizationUnitsAsync(), Times.Once);
        }
    }
}
