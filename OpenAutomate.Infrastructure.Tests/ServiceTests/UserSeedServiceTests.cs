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

public class UserSeedServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<ILogger<UserSeedService>> _mockLogger;
    private readonly Mock<IOptions<UserSeedSettings>> _mockOptions;
    private readonly UserSeedSettings _userSeedSettings;
    private readonly UserSeedService _userSeedService;

    public UserSeedServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockLogger = new Mock<ILogger<UserSeedService>>();
        _mockOptions = new Mock<IOptions<UserSeedSettings>>();

        _userSeedSettings = new UserSeedSettings
        {
            EnableSeeding = true,
            Users = new List<UserSeedAccount>
            {
                new UserSeedAccount
                {
                    Email = "admin@test.com",
                    Password = "testPassword123",
                    SystemRole = SystemRole.Admin,
                    FirstName = "Test",
                    LastName = "Admin"
                },
                new UserSeedAccount
                {
                    Email = "user1@test.com",
                    Password = "testPassword123",
                    SystemRole = SystemRole.User,
                    FirstName = "Test",
                    LastName = "User1"
                }
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_userSeedSettings);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _userSeedService = new UserSeedService(_mockUnitOfWork.Object, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task SeedUsersAsync_WhenSeedingDisabled_ReturnsZero()
    {
        // Arrange
        _userSeedSettings.EnableSeeding = false;

        // Act
        var result = await _userSeedService.SeedUsersAsync();

        // Assert
        Assert.Equal(0, result);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task SeedUsersAsync_WhenNoUsersConfigured_ReturnsZero()
    {
        // Arrange
        _userSeedSettings.Users = new List<UserSeedAccount>();

        // Act
        var result = await _userSeedService.SeedUsersAsync();

        // Assert
        Assert.Equal(0, result);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task SeedUsersAsync_WhenUsersDoNotExist_CreatesUsersAndReturnsCount()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                          .ReturnsAsync((User?)null);
        _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        // Act
        var result = await _userSeedService.SeedUsersAsync();

        // Assert
        Assert.Equal(2, result);
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == "admin@test.com" &&
            u.FirstName == "Test" &&
            u.LastName == "Admin" &&
            u.SystemRole == SystemRole.Admin &&
            u.IsEmailVerified == true &&
            !string.IsNullOrEmpty(u.PasswordHash) &&
            !string.IsNullOrEmpty(u.PasswordSalt)
        )), Times.Once);
        
        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == "user1@test.com" &&
            u.FirstName == "Test" &&
            u.LastName == "User1" &&
            u.SystemRole == SystemRole.User &&
            u.IsEmailVerified == true &&
            !string.IsNullOrEmpty(u.PasswordHash) &&
            !string.IsNullOrEmpty(u.PasswordSalt)
        )), Times.Once);
        
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SeedUsersAsync_WhenUsersAlreadyExist_SkipsExistingUsers()
    {
        // Arrange
        var existingUser = new User { Email = "admin@test.com" };
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.Is<Expression<Func<User, bool>>>(
            expr => expr.Compile()(existingUser))))
                          .ReturnsAsync(existingUser);
        
        _mockUserRepository.Setup(x => x.GetFirstOrDefaultAsync(It.Is<Expression<Func<User, bool>>>(
            expr => !expr.Compile()(existingUser))))
                          .ReturnsAsync((User?)null);
        
        _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

        // Act
        var result = await _userSeedService.SeedUsersAsync();

        // Assert
        Assert.Equal(1, result); // Only one user should be seeded
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedUsersAsync_WhenUserConfigurationInvalid_SkipsInvalidUsers()
    {
        // Arrange
        _userSeedSettings.Users = new List<UserSeedAccount>
        {
            new UserSeedAccount
            {
                Email = "", // Invalid email
                Password = "testPassword123",
                SystemRole = SystemRole.Admin,
                FirstName = "Test",
                LastName = "Admin"
            },
            new UserSeedAccount
            {
                Email = "valid@test.com",
                Password = "", // Invalid password
                SystemRole = SystemRole.User,
                FirstName = "Test",
                LastName = "User"
            }
        };

        // Act
        var result = await _userSeedService.SeedUsersAsync();

        // Assert
        Assert.Equal(0, result);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Never);
    }
}
