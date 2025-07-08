using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.Dto.UserDto;
using OpenAutomate.Core.Exceptions;
using OpenAutomate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class AccountServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
        private readonly Mock<ILogger<AccountService>> _mockLogger = new();
        private readonly Mock<IRepository<User>> _mockUserRepository = new();
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);
            _accountService = new AccountService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

       

        [Fact]
        public async Task GetUserProfileAsync_WithInvalidUserId_ThrowsServiceException()
        {
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<ServiceException>(() => _accountService.GetUserProfileAsync(userId));
        }

        [Fact]
        public async Task UpdateUserInfoAsync_WithValidRequest_UpdatesUser()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FirstName = "A", LastName = "B", SystemRole = SystemRole.User };
            var request = new UpdateUserInfoRequest { FirstName = "C", LastName = "D" };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            var result = await _accountService.UpdateUserInfoAsync(userId, request);

            Assert.NotNull(result);
            Assert.Equal("C", result.FirstName);
            Assert.Equal("D", result.LastName);
        }

        [Fact]
        public async Task UpdateUserInfoAsync_WithInvalidUserId_ThrowsServiceException()
        {
            var userId = Guid.NewGuid();
            var request = new UpdateUserInfoRequest { FirstName = "C", LastName = "D" };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<ServiceException>(() => _accountService.UpdateUserInfoAsync(userId, request));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidRequest_ChangesPassword()
        {
            var userId = Guid.NewGuid();
            CreatePasswordHashForTest("oldpass", out string hash, out string salt);

            var user = new User
            {
                Id = userId,
                FirstName = "A",
                LastName = "B",
                SystemRole = SystemRole.User,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpass",
                NewPassword = "newpass",
                ConfirmNewPassword = "newpass"
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            var result = await _accountService.ChangePasswordAsync(userId, request);

            Assert.True(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithInvalidUserId_ThrowsServiceException()
        {
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpass",
                NewPassword = "newpass",
                ConfirmNewPassword = "newpass"
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<ServiceException>(() => _accountService.ChangePasswordAsync(userId, request));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsServiceException()
        {
            var userId = Guid.NewGuid();
            CreatePasswordHashForTest("oldpass", out string hash, out string salt);

            var user = new User
            {
                Id = userId,
                FirstName = "A",
                LastName = "B",
                SystemRole = SystemRole.User,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "wrongpass",
                NewPassword = "newpass",
                ConfirmNewPassword = "newpass"
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            await Assert.ThrowsAsync<ServiceException>(() => _accountService.ChangePasswordAsync(userId, request));
        }
        [Fact]
        public async Task ChangePasswordAsync_WithInvalidNewPassword_DoesNotThrow()
        {
            var userId = Guid.NewGuid();
            CreatePasswordHashForTest("oldpass", out string hash, out string salt);

            var user = new User
            {
                Id = userId,
                FirstName = "A",
                LastName = "B",
                SystemRole = SystemRole.User,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "oldpass",
                NewPassword = "short",
                ConfirmNewPassword = "short"
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            var result = await _accountService.ChangePasswordAsync(userId, request);

            Assert.True(result);
        }


        // Helper method for test password hashing (copy from production)
        private static void CreatePasswordHashForTest(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            byte[] saltBytes = hmac.Key;
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }
    }
}
