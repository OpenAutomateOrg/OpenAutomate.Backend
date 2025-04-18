﻿using OpenAutomate.Core.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Domain.Entities
{
    public class User : BaseUser
    {
        public string? FirstName { set; get; }
        public string? LastName { set; get; }
        public string? ImageUrl { set; get; }
        public List<RefreshToken>? RefreshTokens { get; set; }
        public List<OrganizationUnitUser>? OrganizationUnitUsers { get; set; }
        
        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}
