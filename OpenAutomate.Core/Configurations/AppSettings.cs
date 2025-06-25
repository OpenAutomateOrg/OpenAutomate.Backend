namespace OpenAutomate.Core.Configurations;

/// <summary>
/// Main application settings container
/// </summary>
public class AppSettings
{
    public string FrontendUrl { get; set; } = string.Empty;
    public JwtSettings Jwt { get; set; } = new JwtSettings();
    public DatabaseSettings Database { get; set; } = new DatabaseSettings();
    public CorsSettings Cors { get; set; } = new CorsSettings();
    public RedisSettings Redis { get; set; } = new RedisSettings();
} 