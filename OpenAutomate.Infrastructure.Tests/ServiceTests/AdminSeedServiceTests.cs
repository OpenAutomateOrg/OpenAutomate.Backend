using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Infrastructure.Services;
using System.Linq.Expressions;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests;

public class AdminSeedServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<AdminSeedService>> _mockLogger;
    private readonly Mock<IOptions<AdminSeedSettings>> _mockOptions;
    private readonly AdminSeedService _adminSeedService;
    private readonly AdminSeedSettings _adminSeedSettings;

    public AdminSeedServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AdminSeedService>>();
        _mockOptions = new Mock<IOptions<AdminSeedSettings>>();

        _adminSeedSettings = new AdminSeedSettings
        {
            Email = "admin@test.com",
            Password = "testPassword123",
            FirstName = "Test",
            LastName = "Admin",
            EnableSeeding = true
        };

        _mockOptions.Setup(x => x.Value).Returns(_adminSeedSettings);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _adminSeedService = new AdminSeedService(_mockUnitOfWork.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task SeedSystemAdminAsync_WhenSeedingDisabled_ReturnsFalse()
    {
        // Arrange
        _adminSeedSettings.EnableSeeding = false;

        // Act
        var result = await _adminSeedService.SeedSystemAdminAsync();

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task SeedSystemAdminAsync_WhenAdminExists_ReturnsFalse()
    {
        // Arrange
        var existingUser = new User { Email = _adminSeedSettings.Email };
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                          .ReturnsAsync(existingUser);

        // Act
        var result = await _adminSeedService.SeedSystemAdminAsync();

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task SeedSystemAdminAsync_WhenAdminDoesNotExist_CreatesAdminAndReturnsTrue()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                          .ReturnsAsync((User?)null);
        _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        // Act
        var result = await _adminSeedService.SeedSystemAdminAsync();

        // Assert
        Assert.True(result);
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == _adminSeedSettings.Email &&
            u.FirstName == _adminSeedSettings.FirstName &&
            u.LastName == _adminSeedSettings.LastName &&
            u.SystemRole == SystemRole.Admin &&
            u.IsEmailVerified == true &&
            !string.IsNullOrEmpty(u.PasswordHash) &&
            !string.IsNullOrEmpty(u.PasswordSalt)
        )), Times.Once);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedSystemAdminAsync_WhenExceptionThrown_RethrowsException()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                          .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _adminSeedService.SeedSystemAdminAsync());
    }
}