﻿
namespace OpenAutomate.Domain.BaseEntity
{
    public class BaseUser : BaseEntity
    {
        public string? Login { set; get; }
        public string? PasswordHash { set; get; }
        public string? Email { set; get; }


    }
}
