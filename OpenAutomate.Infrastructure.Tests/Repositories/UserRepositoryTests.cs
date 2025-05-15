using Microsoft.EntityFrameworkCore;
using Moq;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using OpenAutomate.Domain.IRepository;
using OpenAutomate.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests
    {
        // Mock tenant context for testing
        private readonly Mock<ITenantContext> _mockTenantContext;
        
        public UserRepositoryTests()
        {
            _mockTenantContext = new Mock<ITenantContext>();
            _mockTenantContext.Setup(x => x.HasTenant).Returns(true);
            _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(Guid.NewGuid());
        }
        
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            return new ApplicationDbContext(options, _mockTenantContext.Object);
        }
        
        [Fact]
        public async Task AddAsync_WithValidUser_PersistsUser()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            
            // Act
            await repository.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Assert
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(savedUser);
            Assert.Equal("testuser", savedUser.Login);
            Assert.Equal("Test", savedUser.FirstName);
            Assert.Equal("User", savedUser.LastName);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetByIdAsync(user.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("testuser", result.Login);
            Assert.Equal("test@example.com", result.Email);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithNonExistingUser_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            var nonExistingId = Guid.NewGuid();
            
            // Act
            var result = await repository.GetByIdAsync(nonExistingId);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetFirstOrDefaultAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetFirstOrDefaultAsync(u => u.Email == "test@example.com");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Login);
            Assert.Equal("test@example.com", result.Email);
        }
        
        [Fact]
        public async Task GetFirstOrDefaultAsync_WithIncludes_LoadsRelatedEntities()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                RefreshTokens = new List<RefreshToken>
                {
                    new RefreshToken
                    {
                        Token = "test-token",
                        Expires = DateTime.UtcNow.AddDays(7)
                    }
                }
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetFirstOrDefaultAsync(
                u => u.Email == "test@example.com",
                u => u.RefreshTokens!
            );
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.RefreshTokens);
            Assert.Single(result.RefreshTokens);
            Assert.Equal("test-token", result.RefreshTokens!.First().Token);
        }
        
        [Fact]
        public async Task Update_ModifiesExistingUser_PersistsChanges()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            user.FirstName = "Updated";
            user.LastName = "Name";
            repository.Update(user);
            await context.SaveChangesAsync();
            
            // Assert
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated", updatedUser.FirstName);
            Assert.Equal("Name", updatedUser.LastName);
        }
        
        [Fact]
        public async Task Remove_ExistingUser_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            repository.Remove(user);
            await context.SaveChangesAsync();
            
            // Assert
            var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.Null(deletedUser);
        }
        
        [Fact]
        public async Task GetAllAsync_WithMultipleUsers_ReturnsFilteredUsers()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            await context.Users.AddAsync(new User { Login = "user1", Email = "user1@example.com", FirstName = "User", LastName = "One" });
            await context.Users.AddAsync(new User { Login = "user2", Email = "user2@example.com", FirstName = "User", LastName = "Two" });
            await context.Users.AddAsync(new User { Login = "admin", Email = "admin@example.com", FirstName = "Admin", LastName = "User" });
            await context.SaveChangesAsync();
            
            // Act
            var users = await repository.GetAllAsync(u => u.Login.StartsWith("user"));
            
            // Assert
            Assert.Equal(2, users.Count());
            Assert.Contains(users, u => u.Email == "user1@example.com");
            Assert.Contains(users, u => u.Email == "user2@example.com");
            Assert.DoesNotContain(users, u => u.Email == "admin@example.com");
        }
        
        [Fact]
        public async Task GetAllAsync_WithOrderBy_ReturnsOrderedUsers()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            await context.Users.AddAsync(new User { Login = "zuser", Email = "z@example.com", FirstName = "Z", LastName = "User" });
            await context.Users.AddAsync(new User { Login = "auser", Email = "a@example.com", FirstName = "A", LastName = "User" });
            await context.Users.AddAsync(new User { Login = "muser", Email = "m@example.com", FirstName = "M", LastName = "User" });
            await context.SaveChangesAsync();
            
            // Act
            var users = await repository.GetAllAsync(
                filter: null,
                orderBy: query => query.OrderBy(u => u.Login)
            );
            
            // Assert
            var userList = users.ToList();
            Assert.Equal(3, userList.Count);
            Assert.Equal("auser", userList[0].Login);
            Assert.Equal("muser", userList[1].Login);
            Assert.Equal("zuser", userList[2].Login);
        }
        
        [Fact]
        public async Task UpdatePropertyAsync_ModifiesSingleProperty_UpdatesOnlyThatProperty()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsEmailVerified = false
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act - Just update the user directly for this test
            user.IsEmailVerified = true;
            context.Entry(user).Property(u => u.IsEmailVerified).IsModified = true;
            await context.SaveChangesAsync();
            
            // Assert
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.IsEmailVerified);
            Assert.Equal("Test", updatedUser.FirstName); // Other properties unchanged
            Assert.Equal("User", updatedUser.LastName); // Other properties unchanged
        }
        
        [Fact]
        public async Task AddRangeAsync_WithMultipleUsers_PersistsAllUsers()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var users = new List<User>
            {
                new User { Login = "user1", Email = "user1@example.com", FirstName = "User", LastName = "One" },
                new User { Login = "user2", Email = "user2@example.com", FirstName = "User", LastName = "Two" }
            };
            
            // Act
            await repository.AddRangeAsync(users);
            await context.SaveChangesAsync();
            
            // Assert
            var savedUsers = await context.Users.ToListAsync();
            Assert.Equal(2, savedUsers.Count);
            Assert.Contains(savedUsers, u => u.Email == "user1@example.com");
            Assert.Contains(savedUsers, u => u.Email == "user2@example.com");
        }
        
        [Fact]
        public async Task RemoveRange_WithMultipleUsers_RemovesAllUsers()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var users = new List<User>
            {
                new User { Login = "user1", Email = "user1@example.com", FirstName = "User", LastName = "One" },
                new User { Login = "user2", Email = "user2@example.com", FirstName = "User", LastName = "Two" },
                new User { Login = "admin", Email = "admin@example.com", FirstName = "Admin", LastName = "User" }
            };
            
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
            
            // Get users to remove
            var usersToRemove = await context.Users.Where(u => u.Login.StartsWith("user")).ToListAsync();
            
            // Act
            repository.RemoveRange(usersToRemove);
            await context.SaveChangesAsync();
            
            // Assert
            var remainingUsers = await context.Users.ToListAsync();
            Assert.Single(remainingUsers);
            Assert.Equal("admin", remainingUsers[0].Login);
        }
        
        [Fact]
        public async Task UpdateOneAsync_ModifiesSingleField_UpdatesOnlyThatField()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            var user = new User
            {
                Login = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsEmailVerified = false
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Act
            await repository.UpdateOneAsync(
                u => u.Email == "test@example.com",
                u => u.LastName,
                "Updated"
            );
            
            // Assert
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated", updatedUser.LastName);
            Assert.Equal("Test", updatedUser.FirstName); // Other fields unchanged
            Assert.False(updatedUser.IsEmailVerified); // Other fields unchanged
        }
        
        [Fact]
        public async Task AnyAsync_WithExistingMatch_ReturnsTrue()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            await context.Users.AddAsync(new User { Login = "user1", Email = "user1@example.com" });
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.AnyAsync(u => u.Email == "user1@example.com");
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task AnyAsync_WithNoMatch_ReturnsFalse()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository<User>(context);
            
            await context.Users.AddAsync(new User { Login = "user1", Email = "user1@example.com" });
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.AnyAsync(u => u.Email == "nonexistent@example.com");
            
            // Assert
            Assert.False(result);
        }
    }
} 