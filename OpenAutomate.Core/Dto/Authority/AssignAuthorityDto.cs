using System;
using System.ComponentModel.DataAnnotations;

namespace OpenAutomate.Core.Dto.Authority
{
    public class AssignAuthorityDto
    {
        [Required]
        public Guid AuthorityId { get; set; }
    }
} 