using System;
using OpenAutomate.Core.Domain.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class UserAuthority : ITenantEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public Guid AuthorityId { get; set; }

        [ForeignKey("AuthorityId")]
        public Authority Authority { get; set; } = null!;

        [Required]
        public Guid OrganizationUnitId { get; set; }

        [ForeignKey("OrganizationUnitId")]
        public OrganizationUnit OrganizationUnit { get; set; } = null!;
    }
}