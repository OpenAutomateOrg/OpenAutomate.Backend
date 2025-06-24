using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class CustomControllerBaseTests
    {
        private class TestController : CustomControllerBase
        {
            public Guid ExposeGetCurrentUserId()
            {
                return GetCurrentUserId();
            }

            public User? ExposeCurrentUser => currentUser;
        }

        private readonly TestController _controller;
        private readonly DefaultHttpContext _httpContext;

        public CustomControllerBaseTests()
        {
            _httpContext = new DefaultHttpContext();
            _controller = new TestController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Fact]
        public void CurrentUser_WhenUserInHttpContext_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _httpContext.Items["User"] = user;

            // Act
            var result = _controller.ExposeCurrentUser;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
        }

        [Fact]
        public void CurrentUser_WhenNoUserInHttpContext_ReturnsNull()
        {
            // Arrange
            _httpContext.Items["User"] = null;

            // Act
            var result = _controller.ExposeCurrentUser;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUserId_WhenUserInHttpContext_ReturnsUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "test@example.com"
            };

            _httpContext.Items["User"] = user;

            // Act
            var result = _controller.ExposeGetCurrentUserId();

            // Assert
            Assert.Equal(userId, result);
        }

        [Fact]
        public void GetCurrentUserId_WhenNoUserInHttpContext_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _httpContext.Items["User"] = null;

            // Act & Assert
            var exception = Assert.Throws<UnauthorizedAccessException>(() => _controller.ExposeGetCurrentUserId());
            Assert.Equal("User is not authenticated", exception.Message);
        }
    }
} 