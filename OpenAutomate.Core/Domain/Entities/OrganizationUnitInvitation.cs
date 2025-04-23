using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUnitInvitation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrganizationUnitId { get; set; } 

        [ForeignKey("OrganizationUnitId")]
        public virtual OrganizationUnit OrganizationUnit { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Guid? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

        public DateTime? AcceptedAt { get; set; }
    }
}
