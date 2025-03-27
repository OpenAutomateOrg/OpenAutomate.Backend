namespace OpenAutomate.Domain.Constants
{
    public class AppSettings
    {
        public static string Secret { get; set; }
        public static int RefreshTokenTTL { get; set; }
        public static string[] CORS { get; set; }
    }
}
