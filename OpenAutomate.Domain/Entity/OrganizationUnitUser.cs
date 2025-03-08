using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Domain.Entity
{
    public class OrganizationUnitUser
    {
        [Required]
        public Guid UserId { set; get; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public Guid OrganizationUnitId { set; get; }
        [ForeignKey("OrganizationUnitId")]
        public OrganizationUnit OrganizationUnit { get; set; }


    }
}
