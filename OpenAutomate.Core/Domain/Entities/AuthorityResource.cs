using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAutomate.Core.Domain.Base;

namespace OpenAutomate.Core.Domain.Entities
{
    public class AuthorityResource : TenantEntity
    {
        [Required]
        public Guid AuthorityId { get; set; }
        
        [ForeignKey("AuthorityId")]
        public Authority Authority { get; set; }
        
        [Required]
        public string ResourceName { get; set; }
        
        [Required]
        public int Permission { get; set; }
    }
} 