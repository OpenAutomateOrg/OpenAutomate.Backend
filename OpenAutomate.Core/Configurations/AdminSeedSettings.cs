namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Configuration settings for seeding the initial system administrator account
/// </summary>
public class AdminSeedSettings
{
    /// <summary>
    /// Email address for the system administrator account
    /// </summary>
    public string Email { get; set; } = "admin@openautomate.io";
    
    /// <summary>
    /// Password for the system administrator account
    /// </summary>
    public string Password { get; set; } = "openAutomate@12345";
    
    /// <summary>
    /// First name for the system administrator account
    /// </summary>
    public string FirstName { get; set; } = "System";
    
    /// <summary>
    /// Last name for the system administrator account
    /// </summary>
    public string LastName { get; set; } = "Administrator";
    
    /// <summary>
    /// Whether to enable automatic seeding of admin account on startup
    /// </summary>
    public bool EnableSeeding { get; set; } = true;
}