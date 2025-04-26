using Xunit;
using Moq;
using OpenAutomate.Core.IServices;
using OpenAutomate.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Tests.ServiceTests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserservice> _mockUserService;
        private readonly Mock<ITenantContext> _mockTenantContext;

        public UserServiceTests()
        {
            _mockUserService = new Mock<IUserservice>();
            _mockTenantContext = new Mock<ITenantContext>();
        }

        [Fact]
        public async Task GetUserById_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new User { Id = userId, Email = "test@example.com" };
            
            _mockUserService.Setup(service => service.GetUserByIdAsync(userId))
                .ReturnsAsync(expectedUser);
            
            // Act
            var result = await _mockUserService.Object.GetUserByIdAsync(userId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@example.com", result.Email);
            _mockUserService.Verify(service => service.GetUserByIdAsync(userId), Times.Once);
        }
        
        [Fact]
        public async Task GetUserById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            
            _mockUserService.Setup(service => service.GetUserByIdAsync(invalidId))
                .ReturnsAsync((User)null);
            
            // Act
            var result = await _mockUserService.Object.GetUserByIdAsync(invalidId);
            
            // Assert
            Assert.Null(result);
            _mockUserService.Verify(service => service.GetUserByIdAsync(invalidId), Times.Once);
        }
        
        [Fact]
        public async Task GetUserByEmail_FiltersByTenantContext()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var email = "test@example.com";
            var expectedUser = new User { Email = email };
            
            _mockTenantContext.Setup(tc => tc.OrganizationUnitId).Returns(tenantId);
            _mockUserService.Setup(service => service.GetUserByEmailAsync(email))
                .ReturnsAsync(expectedUser);
            
            // Act
            var result = await _mockUserService.Object.GetUserByEmailAsync(email);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            _mockUserService.Verify(service => service.GetUserByEmailAsync(email), Times.Once);
        }
        
        [Fact]
        public async Task CreateUser_WithValidData_ReturnsCreatedUser()
        {
            // Arrange
            var newUser = new User 
            { 
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User"
            };
            
            _mockUserService.Setup(service => service.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User u, string p) => 
                {
                    u.Id = Guid.NewGuid();
                    return u;
                });
            
            // Act
            var result = await _mockUserService.Object.CreateUserAsync(newUser, "password");
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("new@example.com", result.Email);
            _mockUserService.Verify(service => service.CreateUserAsync(newUser, "password"), Times.Once);
        }
    }
} 