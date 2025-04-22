using OpenAutomate.Core.Domain.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Authority : BaseEntity
    {
        [Required]
        public string Name { set; get; }
        
        // Navigation properties
        public ICollection<AuthorityResource> AuthorityResources { get; set; }
        public ICollection<UserAuthority> UserAuthorities { get; set; }
    }
}
