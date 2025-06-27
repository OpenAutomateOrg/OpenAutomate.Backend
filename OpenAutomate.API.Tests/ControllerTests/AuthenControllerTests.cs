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
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthenController>> _mockLogger;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly AuthenController _controller;
        private readonly Dictionary<string, string> _cookies;
        private readonly Mock<IRequestCookieCollection> _mockCookieCollection;

        public AuthenControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthenController>>();
            _mockTenantContext = new Mock<ITenantContext>();
            _cookies = new Dictionary<string, string>();

            // Create mock cookie collection that reads from the dictionary
            _mockCookieCollection = new Mock<IRequestCookieCollection>();
            _mockCookieCollection.Setup(c => c["refreshToken"]).Returns(() =>
                _cookies.ContainsKey("refreshToken") ? _cookies["refreshToken"] : null);

            // Create mock HTTP context with Request
            var mockHttpContext = new DefaultHttpContext();
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Cookies).Returns(_mockCookieCollection.Object);

            var controllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext
            };

            // Replace the default request with our mock
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);
            mockHttpContext.Request.Cookies = _mockCookieCollection.Object;

            _controller = new AuthenController(
                _mockAuthService.Object,
                _mockLogger.Object,
                _mockTenantContext.Object)
            {
                ControllerContext = controllerContext
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

            _mockAuthService
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
            // Arrange
            AuthenticationRequest? request = null;

            // Act
#pragma warning disable CS8604 // Possible null reference argument.
            var result = await _controller.Login(request);
#pragma warning restore CS8604 // Possible null reference argument.

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("cannot be null", badRequestResult.Value.ToString());
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
            Assert.Contains("Email is required", badRequestResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new AuthenticationException("Invalid email or password"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Invalid email or password", badRequestResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new EmailVerificationRequiredException("Email not verified"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Email not verified", badRequestResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value.ToString());
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

            // Set up the mock cookie
            _cookies["refreshToken"] = refreshToken;

            _mockAuthService
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
            // Arrange - cookie not set
            
            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            Assert.Contains("Refresh token is required", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsErrorResult()
        {
            // Arrange
            var refreshToken = "invalid.refresh.token";
            _cookies["refreshToken"] = refreshToken;

            _mockAuthService
                .Setup(s => s.RefreshTokenAsync(refreshToken, It.IsAny<string>()))
                .ThrowsAsync(new SecurityTokenException("Invalid token"));

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            // The controller returns ObjectResult with 500 status code for SecurityTokenException
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
            Assert.Contains("An error occurred", objectResult.Value.ToString());
        }

        [Fact]
        public async Task RefreshToken_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var refreshToken = "valid.refresh.token";
            _cookies["refreshToken"] = refreshToken;

            _mockAuthService
                .Setup(s => s.RefreshTokenAsync(refreshToken, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains("Token revoked", okResult.Value.ToString());
            
            // Verify the tenant context was used
            _mockTenantContext.Verify(tc => tc.SetTenant(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task RevokeToken_WithMissingTokenInRequest_UsesCookieToken()
        {
            // Arrange
            var request = new RevokeTokenRequest { Reason = "User logout" };
            var cookieToken = "cookie.refresh.token";
            
            // Set up cookie
            _cookies["refreshToken"] = cookieToken;

            _mockAuthService
                .Setup(s => s.RevokeTokenAsync(cookieToken, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains("Token revoked", okResult.Value.ToString());
            
            // Verify service was called with the cookie token
            _mockAuthService.Verify(s => s.RevokeTokenAsync(cookieToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
            Assert.Contains("Token is required", badRequestResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            Assert.Contains("Token not found", notFoundResult.Value.ToString());
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

            _mockAuthService
                .Setup(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(statusCodeResult.Value);
            Assert.Contains("An error occurred", statusCodeResult.Value.ToString());
        }

        #endregion
    }
}
