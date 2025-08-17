namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Settings for cookie configuration
/// </summary>
public class CookieSettings
{
    /// <summary>
    /// Cookie domain for cross-subdomain access
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// SameSite cookie attribute
    /// </summary>
    public string SameSite { get; set; } = "None";
    
    /// <summary>
    /// Whether cookies should be secure (HTTPS only)
    /// </summary>
    public bool Secure { get; set; } = true;
}