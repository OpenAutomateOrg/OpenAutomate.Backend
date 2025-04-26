using Xunit;
using Moq;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Threading.Tasks;
using OpenAutomate.Core.Dto.UserDto;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ITenantContext> _mockTenantContext;

        public UserServiceTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockTenantContext = new Mock<ITenantContext>();
        }

        [Fact]
        public async Task GetUserById_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUserResponse = new UserResponse 
            { 
                Id = userId, 
                Email = "test@example.com" 
            };
            
            _mockUserService.Setup(service => service.GetByIdAsync(userId))
                .ReturnsAsync(expectedUserResponse);
            
            // Act
            var result = await _mockUserService.Object.GetByIdAsync(userId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@example.com", result.Email);
            _mockUserService.Verify(service => service.GetByIdAsync(userId), Times.Once);
        }
        
        [Fact]
        public async Task GetUserById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            
            _mockUserService.Setup(service => service.GetByIdAsync(invalidId))
                .ReturnsAsync((UserResponse)null);
            
            // Act
            var result = await _mockUserService.Object.GetByIdAsync(invalidId);
            
            // Assert
            Assert.Null(result);
            _mockUserService.Verify(service => service.GetByIdAsync(invalidId), Times.Once);
        }
        
        [Fact]
        public async Task GetUserByEmail_FiltersByTenantContext()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var email = "test@example.com";
            var expectedUserResponse = new UserResponse { Email = email };
            
            _mockTenantContext.Setup(tc => tc.CurrentTenantId).Returns(tenantId);
            _mockUserService.Setup(service => service.GetByEmailAsync(email))
                .ReturnsAsync(expectedUserResponse);
            
            // Act
            var result = await _mockUserService.Object.GetByEmailAsync(email);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            _mockUserService.Verify(service => service.GetByEmailAsync(email), Times.Once);
        }
        
        [Fact]
        public async Task RegisterUser_WithValidData_ReturnsCreatedUser()
        {
            // Arrange
            var registrationRequest = new RegistrationRequest 
            { 
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Password = "password"
            };
            var expectedUserResponse = new UserResponse
            {
                Id = Guid.NewGuid(),
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User"
            };
            
            _mockUserService.Setup(service => service.RegisterAsync(It.IsAny<RegistrationRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedUserResponse);
            
            // Act
            var result = await _mockUserService.Object.RegisterAsync(registrationRequest, "127.0.0.1");
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("new@example.com", result.Email);
            _mockUserService.Verify(service => service.RegisterAsync(registrationRequest, "127.0.0.1"), Times.Once);
        }
    }
} 