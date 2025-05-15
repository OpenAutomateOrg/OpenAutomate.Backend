using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Dto.AdminDto;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class AdminControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _controller = new AdminController(_mockAdminService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOkResult_WithUsers()
        {
            // Arrange
            var expectedUsers = new List<UserResponse>
            {
                new UserResponse { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                new UserResponse { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" }
            };
            
            _mockAdminService.Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(expectedUsers);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<UserResponse>>(okResult.Value);
            Assert.Equal(expectedUsers, returnValue);
        }

        [Fact]
        public async Task GetUserById_WhenUserExists_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new UserResponse { Id = userId, FirstName = "John", LastName = "Doe" };
            
            _mockAdminService.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(expectedUser, returnValue);
        }

        [Fact]
        public async Task GetUserById_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _mockAdminService.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync((UserResponse)null);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUserInfo_WhenValid_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateUserInfoRequest { FirstName = "John", LastName = "Doe" };
            var expectedResponse = new UserResponse { Id = userId, FirstName = "John", LastName = "Doe" };
            
            _mockAdminService.Setup(x => x.UpdateUserInfoAsync(userId, request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateUserInfo(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(expectedResponse, returnValue);
        }

        [Fact]
        public async Task UpdateUserInfo_WhenUserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateUserInfoRequest { FirstName = "John", LastName = "Doe" };
            
            _mockAdminService.Setup(x => x.UpdateUserInfoAsync(userId, request))
                .ThrowsAsync(new ServiceException("User not found"));

            // Act
            var result = await _controller.UpdateUserInfo(userId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            // Convert the object to a JSON string and then to a JObject for flexible property access
            var jsonStr = JsonConvert.SerializeObject(notFoundResult.Value);
            var jsonObj = JObject.Parse(jsonStr);
            
            // Check if the message property exists in any format (camelCase or PascalCase)
            var messageValue = jsonObj["message"] ?? jsonObj["Message"];
            Assert.NotNull(messageValue);
            Assert.Equal("User not found", messageValue.ToString());
        }

        [Fact]
        public async Task ChangePassword_WhenValid_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new AdminChangePasswordRequest 
            { 
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };
            
            _mockAdminService.Setup(x => x.ChangePasswordAsync(userId, request.NewPassword))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Convert the object to a JSON string and then to a JObject for flexible property access
            var jsonStr = JsonConvert.SerializeObject(okResult.Value);
            var jsonObj = JObject.Parse(jsonStr);
            
            // Check if the message property exists in any format (camelCase or PascalCase)
            var messageValue = jsonObj["message"] ?? jsonObj["Message"];
            Assert.NotNull(messageValue);
            Assert.Equal("Password changed successfully", messageValue.ToString());
        }

        [Fact]
        public async Task ChangePassword_WhenPasswordsDoNotMatch_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new AdminChangePasswordRequest 
            { 
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "DifferentPassword123!"
            };

            // Act
            var result = await _controller.ChangePassword(userId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            
            // Convert the object to a JSON string and then to a JObject for flexible property access
            var jsonStr = JsonConvert.SerializeObject(badRequestResult.Value);
            var jsonObj = JObject.Parse(jsonStr);
            
            // Check if the message property exists in any format (camelCase or PascalCase)
            var messageValue = jsonObj["message"] ?? jsonObj["Message"];
            Assert.NotNull(messageValue);
            Assert.Equal("New password and confirm password do not match.", messageValue.ToString());
        }

        [Fact]
        public async Task ChangePassword_WhenUserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new AdminChangePasswordRequest 
            { 
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };
            
            _mockAdminService.Setup(x => x.ChangePasswordAsync(userId, request.NewPassword))
                .ThrowsAsync(new ServiceException("User not found"));

            // Act
            var result = await _controller.ChangePassword(userId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            
            // Convert the object to a JSON string and then to a JObject for flexible property access
            var jsonStr = JsonConvert.SerializeObject(notFoundResult.Value);
            var jsonObj = JObject.Parse(jsonStr);
            
            // Check if the message property exists in any format (camelCase or PascalCase)
            var messageValue = jsonObj["message"] ?? jsonObj["Message"];
            Assert.NotNull(messageValue);
            Assert.Equal("User not found", messageValue.ToString());
        }

        [Fact]
        public async Task GetAllOrganizationUnit_ReturnsOkResult_WithOrganizationUnits()
        {
            // Arrange
            var expectedOUs = new List<OrganizationUnitResponseDto>
            {
                new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "OU1", Description = "Department 1" },
                new OrganizationUnitResponseDto { Id = Guid.NewGuid(), Name = "OU2", Description = "Department 2" }
            };

            // Alternative approach to avoid type mismatch error
            _mockAdminService
                .Setup(x => x.GetAllOrganizationUnitsAsync())
                .Returns(Task.FromResult<IEnumerable<OrganizationUnitResponseDto>>(expectedOUs));

            // Act
            var result = await _controller.GetAllOrganizationUnit();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
} 