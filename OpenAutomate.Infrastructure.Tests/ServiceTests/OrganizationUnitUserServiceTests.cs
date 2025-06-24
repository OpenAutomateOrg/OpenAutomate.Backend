using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.ServiceTests
{
    public class OrganizationUnitUserServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<OrganizationUnit>> _mockOrgUnitRepo;
        private readonly Mock<IRepository<OrganizationUnitUser>> _mockOrgUnitUserRepo;
        private readonly Mock<IRepository<User>> _mockUserRepo;
        private readonly Mock<IRepository<UserAuthority>> _mockUserAuthorityRepo;
        private readonly Mock<IRepository<Authority>> _mockAuthorityRepo;
        private readonly OrganizationUnitUserService _service;

        public OrganizationUnitUserServiceTests()
        {
            _mockOrgUnitRepo = new Mock<IRepository<OrganizationUnit>>();
            _mockOrgUnitUserRepo = new Mock<IRepository<OrganizationUnitUser>>();
            _mockUserRepo = new Mock<IRepository<User>>();
            _mockUserAuthorityRepo = new Mock<IRepository<UserAuthority>>();
            _mockAuthorityRepo = new Mock<IRepository<Authority>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockUnitOfWork.Setup(u => u.OrganizationUnits).Returns(_mockOrgUnitRepo.Object);
            _mockUnitOfWork.Setup(u => u.OrganizationUnitUsers).Returns(_mockOrgUnitUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.UserAuthorities).Returns(_mockUserAuthorityRepo.Object);
            _mockUnitOfWork.Setup(u => u.Authorities).Returns(_mockAuthorityRepo.Object);

            _service = new OrganizationUnitUserService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetUsersInOrganizationUnitAsync_ValidSlug_ReturnsUsers()
        {
            var slug = "org";
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();

            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = slug });

            _mockOrgUnitUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<OrganizationUnitUser, bool>>>(),
                It.IsAny<Func<IQueryable<OrganizationUnitUser>, IOrderedQueryable<OrganizationUnitUser>>>(),
                It.IsAny<Expression<Func<OrganizationUnitUser, object>>[]>()))
                .ReturnsAsync(new List<OrganizationUnitUser> { new OrganizationUnitUser { OrganizationUnitId = orgId, UserId = userId } });

            _mockUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Expression<Func<User, object>>[]>()))
                .ReturnsAsync(new List<User> { new User { Id = userId, Email = "a@b.com" } });

            _mockUserAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<UserAuthority, bool>>>(),
                It.IsAny<Func<IQueryable<UserAuthority>, IOrderedQueryable<UserAuthority>>>(),
                It.IsAny<Expression<Func<UserAuthority, object>>[]>()))
                .ReturnsAsync(new List<UserAuthority> { new UserAuthority { UserId = userId, AuthorityId = authorityId, OrganizationUnitId = orgId } });

            _mockAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Authority, bool>>>(),
                It.IsAny<Func<IQueryable<Authority>, IOrderedQueryable<Authority>>>(),
                It.IsAny<Expression<Func<Authority, object>>[]>()))
                .ReturnsAsync(new List<Authority> { new Authority { Id = authorityId, Name = "Admin" } });

            var result = await _service.GetUsersInOrganizationUnitAsync(slug);

            Assert.Single(result);
            Assert.Equal("a@b.com", result.First().Email);
            Assert.Contains("Admin", result.First().Roles);
        }

        [Fact]
        public async Task GetUsersInOrganizationUnitAsync_InvalidSlug_ReturnsEmpty()
        {
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync((OrganizationUnit)null);

            var result = await _service.GetUsersInOrganizationUnitAsync("invalid");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersInOrganizationUnitAsync_NoUsers_ReturnsEmpty()
        {
            var orgId = Guid.NewGuid();
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = "org" });

            _mockOrgUnitUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<OrganizationUnitUser, bool>>>(),
                It.IsAny<Func<IQueryable<OrganizationUnitUser>, IOrderedQueryable<OrganizationUnitUser>>>(),
                It.IsAny<Expression<Func<OrganizationUnitUser, object>>[]>()))
                .ReturnsAsync(new List<OrganizationUnitUser>());

            var result = await _service.GetUsersInOrganizationUnitAsync("org");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersInOrganizationUnitAsync_UserWithoutRoles_ReturnsUserWithEmptyRoles()
        {
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = "org" });

            _mockOrgUnitUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<OrganizationUnitUser, bool>>>(),
                It.IsAny<Func<IQueryable<OrganizationUnitUser>, IOrderedQueryable<OrganizationUnitUser>>>(),
                It.IsAny<Expression<Func<OrganizationUnitUser, object>>[]>()))
                .ReturnsAsync(new List<OrganizationUnitUser> { new OrganizationUnitUser { OrganizationUnitId = orgId, UserId = userId } });

            _mockUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Expression<Func<User, object>>[]>()))
                .ReturnsAsync(new List<User> { new User { Id = userId, Email = "a@b.com" } });

            _mockUserAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<UserAuthority, bool>>>(),
                It.IsAny<Func<IQueryable<UserAuthority>, IOrderedQueryable<UserAuthority>>>(),
                It.IsAny<Expression<Func<UserAuthority, object>>[]>()))
                .ReturnsAsync(new List<UserAuthority>());

            _mockAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Authority, bool>>>(),
                It.IsAny<Func<IQueryable<Authority>, IOrderedQueryable<Authority>>>(),
                It.IsAny<Expression<Func<Authority, object>>[]>()))
                .ReturnsAsync(new List<Authority>());

            var result = await _service.GetUsersInOrganizationUnitAsync("org");
            Assert.Single(result);
            Assert.Empty(result.First().Roles);
        }

        [Fact]
        public async Task DeleteUserAsync_Valid_ReturnsTrue()
        {
            var slug = "org";
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();

            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = slug });

            _mockOrgUnitUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<OrganizationUnitUser, bool>>>(),
                It.IsAny<Func<IQueryable<OrganizationUnitUser>, IOrderedQueryable<OrganizationUnitUser>>>(),
                It.IsAny<Expression<Func<OrganizationUnitUser, object>>[]>()))
                .ReturnsAsync(new List<OrganizationUnitUser> { new OrganizationUnitUser { OrganizationUnitId = orgId, UserId = userId } });

            _mockUserAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<UserAuthority, bool>>>(),
                It.IsAny<Func<IQueryable<UserAuthority>, IOrderedQueryable<UserAuthority>>>(),
                It.IsAny<Expression<Func<UserAuthority, object>>[]>()))
                .ReturnsAsync(new List<UserAuthority> { new UserAuthority { UserId = userId, AuthorityId = authorityId, OrganizationUnitId = orgId } });

            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var result = await _service.DeleteUserAsync(slug, userId);

            Assert.True(result);
            _mockUserAuthorityRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<UserAuthority>>()), Times.Once);
            _mockOrgUnitUserRepo.Verify(r => r.Remove(It.IsAny<OrganizationUnitUser>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_InvalidSlug_ReturnsFalse()
        {
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync((OrganizationUnit)null);

            var result = await _service.DeleteUserAsync("invalid", Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUserAsync_UserNotInOrg_ReturnsFalse()
        {
            var orgId = Guid.NewGuid();
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = "org" });

            _mockOrgUnitUserRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<OrganizationUnitUser, bool>>>(),
                It.IsAny<Func<IQueryable<OrganizationUnitUser>, IOrderedQueryable<OrganizationUnitUser>>>(),
                It.IsAny<Expression<Func<OrganizationUnitUser, object>>[]>()))
                .ReturnsAsync(new List<OrganizationUnitUser>());

            var result = await _service.DeleteUserAsync("org", Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task GetRolesInOrganizationUnitAsync_ValidSlug_ReturnsRoles()
        {
            var slug = "org";
            var orgId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();

            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = slug });

            _mockAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Authority, bool>>>(),
                It.IsAny<Func<IQueryable<Authority>, IOrderedQueryable<Authority>>>(),
                It.IsAny<Expression<Func<Authority, object>>[]>()))
                .ReturnsAsync(new List<Authority> { new Authority { Id = authorityId, Name = "Admin", Description = "Quản trị", OrganizationUnitId = orgId } });

            var result = await _service.GetRolesInOrganizationUnitAsync(slug);

            Assert.Single(result);
            Assert.Equal("Admin", result.First().Name);
        }

        [Fact]
        public async Task GetRolesInOrganizationUnitAsync_InvalidSlug_ReturnsEmpty()
        {
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync((OrganizationUnit)null);

            var result = await _service.GetRolesInOrganizationUnitAsync("invalid");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRolesInOrganizationUnitAsync_NoRoles_ReturnsEmpty()
        {
            var orgId = Guid.NewGuid();
            _mockOrgUnitRepo.Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<OrganizationUnit, bool>>>(),
                It.IsAny<Expression<Func<OrganizationUnit, object>>[]>()))
                .ReturnsAsync(new OrganizationUnit { Id = orgId, Slug = "org" });

            _mockAuthorityRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Authority, bool>>>(),
                It.IsAny<Func<IQueryable<Authority>, IOrderedQueryable<Authority>>>(),
                It.IsAny<Expression<Func<Authority, object>>[]>()))
                .ReturnsAsync(new List<Authority>());

            var result = await _service.GetRolesInOrganizationUnitAsync("org");
            Assert.Empty(result);
        }
    }
}
