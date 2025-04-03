using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUser
    {
        [Required]
        public Guid UserId { set; get; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public Guid OrganizationId { set; get; }
        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }


    }
}
