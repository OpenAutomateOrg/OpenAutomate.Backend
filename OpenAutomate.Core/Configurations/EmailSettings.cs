namespace OpenAutomate.Core.Configurations
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "email-smtp.ap-southeast-1.amazonaws.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; }
        public string SenderName { get; set; } = "OpenAutomate";
        public string Username { get; set; }
        public string Password { get; set; }
    }
} 