namespace OpenAutomate.Core.Domain.BaseEntity
{
    public class BaseUser : BaseEntity
    {
        public string? Login { set; get; }
        public string? PasswordHash { set; get; }
        public string? PasswordSalt { set; get; }
        public string? Email { set; get; }


    }
}
