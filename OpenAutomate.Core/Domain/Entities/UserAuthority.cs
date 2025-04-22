using OpenAutomate.Core.Domain.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class UserAuthority : BaseEntity
    {
        [Required]
        public Guid UserId { set; get; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public Guid AuthorityId { set; get; }
        
        [ForeignKey("AuthorityId")]
        public Authority Authority { get; set; }
        
        [Required]
        public Guid OrganizationUnitId { get; set; }
        
        [ForeignKey("OrganizationUnitId")]
        public OrganizationUnit OrganizationUnit { get; set; }
    }
}
