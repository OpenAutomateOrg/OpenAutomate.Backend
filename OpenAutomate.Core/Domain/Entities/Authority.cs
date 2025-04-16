using OpenAutomate.Core.Domain.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class Authority : BaseEntity
    {
        [Required]
        public string Name { set; get; }
        
        [Required]
        public Guid OrganizationUnitId { get; set; }
        
        [ForeignKey("OrganizationUnitId")]
        public OrganizationUnit OrganizationUnit { get; set; }
        
        // Navigation properties
        public ICollection<AuthorityResource> AuthorityResources { get; set; }
        public ICollection<UserAuthority> UserAuthorities { get; set; }
    }
}
