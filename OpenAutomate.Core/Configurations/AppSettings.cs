namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Main application settings container
/// </summary>
public class AppSettings
{
    public JwtSettings Jwt { get; set; }
    public DatabaseSettings Database { get; set; }
    public CorsSettings Cors { get; set; }
} 