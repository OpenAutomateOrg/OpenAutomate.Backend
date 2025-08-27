using OpenAutomate.Core.Domain.Enums;

namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Configuration settings for seeding user accounts on startup
/// </summary>
public class UserSeedSettings
{
    /// <summary>
    /// Whether to enable automatic seeding of user accounts on startup
    /// </summary>
    public bool EnableSeeding { get; set; } = true;
    
    /// <summary>
    /// List of user accounts to seed
    /// </summary>
    public List<UserSeedAccount> Users { get; set; } = new List<UserSeedAccount>();
}

/// <summary>
/// Configuration for an individual user account to be seeded
/// </summary>
public class UserSeedAccount
{
    /// <summary>
    /// Email address for the user account
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for the user account
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// System role for the user account
    /// </summary>
    public SystemRole SystemRole { get; set; } = SystemRole.User;
    
    /// <summary>
    /// First name for the user account
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name for the user account
    /// </summary>
    public string LastName { get; set; } = string.Empty;
}
