using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Service for seeding user accounts on startup
/// </summary>
public class UserSeedService : IUserSeedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserSeedService> _logger;
    private readonly UserSeedSettings _userSeedSettings;

    public UserSeedService(
        IUnitOfWork unitOfWork,
        ILogger<UserSeedService> logger,
        IOptions<UserSeedSettings> userSeedSettings)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _userSeedSettings = userSeedSettings.Value;
    }

    /// <summary>
    /// Seeds user accounts if they don't exist and seeding is enabled
    /// </summary>
    /// <returns>Number of users that were successfully seeded</returns>
    public async Task<int> SeedUsersAsync()
    {
        try
        {
            // Check if seeding is enabled
            if (!_userSeedSettings.EnableSeeding)
            {
                _logger.LogInformation("User seeding is disabled");
                return 0;
            }

            if (_userSeedSettings.Users == null || !_userSeedSettings.Users.Any())
            {
                _logger.LogInformation("No users configured for seeding");
                return 0;
            }

            int seededCount = 0;

            foreach (var userConfig in _userSeedSettings.Users)
            {
                try
                {
                    if (await SeedUserAsync(userConfig))
                    {
                        seededCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while seeding user account: {Email}", userConfig.Email);
                    // Continue with other users instead of failing completely
                }
            }

            _logger.LogInformation("User seeding completed. {SeededCount} users seeded out of {TotalCount} configured", 
                seededCount, _userSeedSettings.Users.Count);

            return seededCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user seeding process");
            throw;
        }
    }

    /// <summary>
    /// Seeds a single user account if it doesn't exist
    /// </summary>
    /// <param name="userConfig">User configuration</param>
    /// <returns>True if user was seeded, false if already exists</returns>
    private async Task<bool> SeedUserAsync(UserSeedAccount userConfig)
    {
        // Validate user configuration
        if (string.IsNullOrWhiteSpace(userConfig.Email))
        {
            _logger.LogWarning("Skipping user seeding: Email is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(userConfig.Password))
        {
            _logger.LogWarning("Skipping user seeding for {Email}: Password is required", userConfig.Email);
            return false;
        }

        // Check if user already exists
        var existingUser = await _unitOfWork.Users.GetFirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == userConfig.Email.ToLower());

        if (existingUser != null)
        {
            _logger.LogInformation("User account already exists with email: {Email}", userConfig.Email);
            return false;
        }

        // Create password hash
        CreatePasswordHash(userConfig.Password, out string passwordHash, out string passwordSalt);

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = userConfig.Email,
            FirstName = userConfig.FirstName,
            LastName = userConfig.LastName,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            SystemRole = userConfig.SystemRole,
            IsEmailVerified = true, // Seeded accounts should be pre-verified
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null // System seeded account
        };

        // Add user to database
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("User account created successfully with email: {Email}, Role: {SystemRole}", 
            userConfig.Email, userConfig.SystemRole);
        
        return true;
    }

    /// <summary>
    /// Creates password hash and salt for a given password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="passwordHash">Generated password hash</param>
    /// <param name="passwordSalt">Generated password salt</param>
    private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
    {
        using var hmac = new HMACSHA512();
        byte[] saltBytes = hmac.Key;
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        // Convert to Base64 strings for storage
        passwordSalt = Convert.ToBase64String(saltBytes);
        passwordHash = Convert.ToBase64String(hashBytes);
    }
}
