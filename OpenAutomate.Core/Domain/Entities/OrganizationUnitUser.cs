using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class OrganizationUnitUser : TenantEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
