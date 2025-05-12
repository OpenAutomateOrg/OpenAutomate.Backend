using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using OpenAutomate.API.Controllers;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Core.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.API.Tests.ControllerTests
{
    public class AuthenControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<AuthenController>> _mockLogger;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly AuthenController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockHttpRequest;
        private readonly Dictionary<string, string> _cookies;

        public AuthenControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<AuthenController>>();
            _mockTenantContext = new Mock<ITenantContext>();
            
            // Setup mock HTTP context and request
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpRequest = new Mock<HttpRequest>();
            _cookies = new Dictionary<string, string>();
            
            var mockCookieCollection = new Mock<IRequestCookieCollection>();
            mockCookieCollection.Setup(c => c["refreshToken"]).Returns(() => 
                _cookies.ContainsKey("refreshToken") ? _cookies["refreshToken"] : null);
            
            _mockHttpRequest.Setup(r => r.Cookies).Returns(mockCookieCollection.Object);
            _mockHttpContext.Setup(c => c.Request).Returns(_mockHttpRequest.Object);
            
            _controller = new AuthenController(
                _mockUserService.Object,
                _mockLogger.Object,
                _mockTenantContext.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var expectedResponse = new AuthenticationResponse
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Token = "valid.access.token",
                RefreshToken = "valid.refresh.token",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
            };

            _mockUserService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthenticationResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.Email, response.Email);
            Assert.Equal(expectedResponse.Token, response.Token);
            
            // Verify the tenant context was used
            _mockTenantContext.Verify(tc => tc.SetTenant(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task Login_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Login(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("cannot be null", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Login_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "",
                Password = "Password123!"
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Email is required", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Login_WhenAuthenticationFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockUserService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new AuthenticationException("Invalid email or password"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Invalid email or password", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Login_WhenEmailNotVerified_ReturnsBadRequest()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _mockUserService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new EmailVerificationRequiredException("Email not verified"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Email not verified", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Login_WhenUnexpectedErrorOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _mockUserService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region RefreshToken Tests

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsOkWithNewToken()
        {
            // Arrange
            var refreshToken = "valid.refresh.token";
            var expectedResponse = new AuthenticationResponse
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Token = "new.access.token",
                RefreshToken = "new.refresh.token",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
            };

            // Set the cookie
            _cookies["refreshToken"] = refreshToken;

            _mockUserService
                .Setup(s => s.RefreshTokenAsync(refreshToken, It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthenticationResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.Email, response.Email);
            Assert.Equal(expectedResponse.Token, response.Token);
            
            // Verify the tenant context was used
            _mockTenantContext.Verify(tc => tc.SetTenant(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
        {
            // Arrange - no cookie set

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Refresh token is required", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var refreshToken = "invalid.refresh.token";
            // Set the cookie
            _cookies["refreshToken"] = refreshToken;

            _mockUserService
                .Setup(s => s.RefreshTokenAsync(refreshToken, It.IsAny<string>()))
                .ThrowsAsync(new SecurityTokenException("Invalid token"));

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Invalid token", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task RefreshToken_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var refreshToken = "valid.refresh.token";
            // Set the cookie
            _cookies["refreshToken"] = refreshToken;

            _mockUserService
                .Setup(s => s.RefreshTokenAsync(refreshToken, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region RevokeToken (Logout) Tests

        [Fact]
        public async Task RevokeToken_WithValidToken_ReturnsOkResult()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                Token = "valid.refresh.token",
                Reason = "User logout"
            };

            _mockUserService
                .Setup(s => s.RevokeTokenAsync(request.Token!, It.IsAny<string>(), request.Reason))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains("Token revoked successfully", okResult.Value?.ToString() ?? string.Empty);
            
            // Verify the tenant context was used
            _mockTenantContext.Verify(tc => tc.SetTenant(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task RevokeToken_WithMissingTokenInRequest_UsesCookieToken()
        {
            // Arrange
            var request = new RevokeTokenRequest { Reason = "User logout" };
            var cookieToken = "cookie.refresh.token";
            
            // Set the cookie
            _cookies["refreshToken"] = cookieToken;

            _mockUserService
                .Setup(s => s.RevokeTokenAsync(cookieToken, It.IsAny<string>(), request.Reason))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains("Token revoked successfully", okResult.Value?.ToString() ?? string.Empty);
            
            // Verify service was called with the cookie token
            _mockUserService.Verify(s => s.RevokeTokenAsync(cookieToken, It.IsAny<string>(), request.Reason), Times.Once);
        }

        [Fact]
        public async Task RevokeToken_WithNoTokenAvailable_ReturnsBadRequest()
        {
            // Arrange
            var request = new RevokeTokenRequest { Reason = "User logout" };
            // No token in request or cookie

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Token is required", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task RevokeToken_WhenTokenNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                Token = "nonexistent.token",
                Reason = "User logout"
            };

            _mockUserService
                .Setup(s => s.RevokeTokenAsync(request.Token!, It.IsAny<string>(), request.Reason))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            Assert.Contains("Token not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task RevokeToken_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                Token = "valid.refresh.token",
                Reason = "User logout"
            };

            _mockUserService
                .Setup(s => s.RevokeTokenAsync(request.Token!, It.IsAny<string>(), request.Reason))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }

        #endregion
    }
}
