namespace OpenAutomate.API.Config;

public class AppSettings
{
    public static string DefaultConnection { get; private set; }
    public static int RefreshTokenTTL { get; set; }
    public static string Secret { get; set; }
}