using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Exceptions;
using Xunit;
using System.Collections.Generic;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class UserControllerTests
    {
        [Fact]
        public async Task UpdateUserInfo_WithValidRequest_ReturnsOkWithUserResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new UpdateUserInfoRequest { FirstName = "John", LastName = "Doe" };
            var expectedResponse = new UserResponse
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                IsEmailVerified = true
            };
            mockUserService.Setup(s => s.UpdateUserInfoAsync(userId, request))
                .ReturnsAsync(expectedResponse);

            var controller = new UserController(mockUserService.Object, mockLogger.Object);

            // Mock HttpContext and set current user
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId, FirstName = "John", LastName = "Doe", IsEmailVerified = true, Email = "john.doe@example.com" };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.UpdateUserInfo(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userResponse = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, userResponse.Id);
            Assert.Equal(expectedResponse.FirstName, userResponse.FirstName);
            Assert.Equal(expectedResponse.LastName, userResponse.LastName);
            Assert.Equal(expectedResponse.Email, userResponse.Email);
            Assert.Equal(expectedResponse.IsEmailVerified, userResponse.IsEmailVerified);
        }

        [Fact]
        public async Task ChangePassword_WithValidRequest_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" };
            mockUserService.Setup(s => s.ChangePasswordAsync(userId, request)).ReturnsAsync(true);

            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var messageProperty = okResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var messageValue = messageProperty.GetValue(okResult.Value) as string;
            Assert.Equal("Password changed successfully", messageValue);
        }

        [Fact]
        public async Task UpdateUserInfo_WithoutAuthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new UpdateUserInfoRequest { FirstName = "John", LastName = "Doe" };

            // Act
            var result = await controller.UpdateUserInfo(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task UpdateUserInfo_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new UpdateUserInfoRequest { FirstName = "John", LastName = "Doe" };
            mockUserService.Setup(s => s.UpdateUserInfoAsync(userId, request)).ThrowsAsync(new Exception("Unexpected error"));
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.UpdateUserInfo(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WhenPasswordChangeFails_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" };
            mockUserService.Setup(s => s.ChangePasswordAsync(userId, request)).ReturnsAsync(false);
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithoutAuthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WhenServiceExceptionThrown_ReturnsBadRequestWithMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" };
            mockUserService.Setup(s => s.ChangePasswordAsync(userId, request)).ThrowsAsync(new ServiceException("Service error"));
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var messageValue = messageProperty.GetValue(badRequestResult.Value) as string;
            Assert.Equal("Service error", messageValue);
        }

        [Fact]
        public async Task ChangePassword_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<UserController>>();
            var request = new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" };
            mockUserService.Setup(s => s.ChangePasswordAsync(userId, request)).ThrowsAsync(new Exception("Unexpected error"));
            var controller = new UserController(mockUserService.Object, mockLogger.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new User { Id = userId };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
} 