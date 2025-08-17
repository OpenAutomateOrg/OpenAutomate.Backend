using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAutomate.Core.Configurations;
using OpenAutomate.Core.Domain.Entities;
using OpenAutomate.Core.Domain.Enums;
using OpenAutomate.Core.Domain.IRepository;
using OpenAutomate.Core.IServices;
using System.Security.Cryptography;
using System.Text;

namespace OpenAutomate.Infrastructure.Services;

/// <summary>
/// Service for seeding the initial system administrator account
/// </summary>
public class AdminSeedService : IAdminSeedService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminSeedService> _logger;
    private readonly AdminSeedSettings _adminSeedSettings;

    public AdminSeedService(
        IUnitOfWork unitOfWork,
        ILogger<AdminSeedService> logger,
        IOptions<AdminSeedSettings> adminSeedSettings)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _adminSeedSettings = adminSeedSettings.Value;
    }

    /// <summary>
    /// Seeds the system administrator account if it doesn't exist and seeding is enabled
    /// </summary>
    /// <returns>True if admin was seeded, false if already exists or seeding is disabled</returns>
    public async Task<bool> SeedSystemAdminAsync()
    {
        try
        {
            // Check if seeding is enabled
            if (!_adminSeedSettings.EnableSeeding)
            {
                _logger.LogInformation("Admin seeding is disabled");
                return false;
            }

            // Check if system admin already exists
            var existingAdmin = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                u => u.Email != null && u.Email.ToLower() == _adminSeedSettings.Email.ToLower());

            if (existingAdmin != null)
            {
                _logger.LogInformation("System admin account already exists with email: {Email}", _adminSeedSettings.Email);
                return false;
            }

            // Create password hash
            CreatePasswordHash(_adminSeedSettings.Password, out string passwordHash, out string passwordSalt);

            // Create new system admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = _adminSeedSettings.Email,
                FirstName = _adminSeedSettings.FirstName,
                LastName = _adminSeedSettings.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                SystemRole = SystemRole.Admin,
                IsEmailVerified = true, // Admin account should be pre-verified
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System seeded account
            };

            // Add user to database
            await _unitOfWork.Users.AddAsync(adminUser);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("System admin account created successfully with email: {Email}", _adminSeedSettings.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding system admin account");
            throw;
        }
    }

    /// <summary>
    /// Creates password hash and salt using HMACSHA512
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="passwordHash">Output password hash</param>
    /// <param name="passwordSalt">Output password salt</param>
    private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
    {
        using var hmac = new HMACSHA512();
        byte[] saltBytes = hmac.Key;
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        passwordSalt = Convert.ToBase64String(saltBytes);
        passwordHash = Convert.ToBase64String(hashBytes);
    }
}